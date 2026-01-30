#!/bin/bash
# =============================================================================
# R2 上传脚本 (Cloudflare R2)
# 用法: ./upload_r2.sh <file_path>
# 
# 环境变量:
#   R2_UPLOADER_URL - R2 Uploader Worker URL (必需)
# =============================================================================

set -e

FILE_PATH="$1"

if [ -z "$FILE_PATH" ]; then
    echo "用法: $0 <file_path>"
    exit 1
fi

if [ ! -f "$FILE_PATH" ]; then
    echo "[ERROR] 文件不存在: $FILE_PATH"
    exit 1
fi

# 加载环境变量
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -f "$SCRIPT_DIR/.env" ]; then
    source "$SCRIPT_DIR/.env"
fi

# 检查 R2 Uploader URL
if [ -z "$R2_UPLOADER_URL" ]; then
    echo "[ERROR] 未配置 R2_UPLOADER_URL 环境变量"
    exit 1
fi

# 绕过代理 (Surge 等) 避免 SSL 错误
unset http_proxy https_proxy HTTP_PROXY HTTPS_PROXY all_proxy ALL_PROXY

FILENAME=$(basename "$FILE_PATH")
FILESIZE=$(stat -f%z "$FILE_PATH" 2>/dev/null || stat -c%s "$FILE_PATH")
CONTENT_TYPE="application/vnd.android.package-archive"

echo "[INFO] 准备上传: $FILENAME ($((FILESIZE / 1024 / 1024)) MB)"

# Step 1: 初始化分片上传
echo "[INFO] Step 1: 初始化上传..."
INIT_RESPONSE=$(curl -s -X POST "${R2_UPLOADER_URL}/api/upload/init" \
    -H "Content-Type: application/json" \
    -d "{\"filename\": \"$FILENAME\", \"contentType\": \"$CONTENT_TYPE\"}")

UPLOAD_ID=$(echo "$INIT_RESPONSE" | grep -o '"uploadId":"[^"]*"' | cut -d'"' -f4)
KEY=$(echo "$INIT_RESPONSE" | grep -o '"key":"[^"]*"' | cut -d'"' -f4)

if [ -z "$UPLOAD_ID" ] || [ -z "$KEY" ]; then
    echo "[ERROR] 初始化失败: $INIT_RESPONSE"
    exit 1
fi

echo "[INFO] Upload ID: $UPLOAD_ID"
echo "[INFO] Key: $KEY"

# Step 2: 分片上传
CHUNK_SIZE=$((10 * 1024 * 1024))  # 10 MB
PARTS_JSON="["
PART_NUM=1
OFFSET=0

while [ $OFFSET -lt $FILESIZE ]; do
    REMAINING=$((FILESIZE - OFFSET))
    if [ $REMAINING -lt $CHUNK_SIZE ]; then
        THIS_CHUNK_SIZE=$REMAINING
    else
        THIS_CHUNK_SIZE=$CHUNK_SIZE
    fi
    
    echo "[INFO] Step 2: 上传分片 $PART_NUM (offset: $OFFSET, size: $THIS_CHUNK_SIZE)..."
    
    # 获取签名 URL
    SIGN_RESPONSE=$(curl -s -X POST "${R2_UPLOADER_URL}/api/upload/sign-part" \
        -H "Content-Type: application/json" \
        -d "{\"key\": \"$KEY\", \"uploadId\": \"$UPLOAD_ID\", \"partNumber\": $PART_NUM}")
    
    SIGNED_URL=$(echo "$SIGN_RESPONSE" | grep -o '"url":"[^"]*"' | cut -d'"' -f4 | sed 's/\\u0026/\&/g')
    
    if [ -z "$SIGNED_URL" ]; then
        echo "[ERROR] 获取签名 URL 失败: $SIGN_RESPONSE"
        exit 1
    fi
    
    # 上传分片
    TEMP_CHUNK="/tmp/chunk_${PART_NUM}.tmp"
    dd if="$FILE_PATH" of="$TEMP_CHUNK" bs=1 skip=$OFFSET count=$THIS_CHUNK_SIZE 2>/dev/null
    
    UPLOAD_RESPONSE=$(curl -s -i -X PUT "$SIGNED_URL" \
        -H "Content-Type: application/octet-stream" \
        --data-binary "@$TEMP_CHUNK")
    
    # 提取 ETag
    ETAG=$(echo "$UPLOAD_RESPONSE" | grep -i "^etag:" | tr -d '\r' | cut -d' ' -f2)
    
    rm -f "$TEMP_CHUNK"
    
    if [ -z "$ETAG" ]; then
        echo "[ERROR] 上传分片失败: $UPLOAD_RESPONSE"
        exit 1
    fi
    
    echo "[INFO] 分片 $PART_NUM 上传成功, ETag: $ETAG"
    
    # 添加到 parts JSON
    if [ $PART_NUM -gt 1 ]; then
        PARTS_JSON+=","
    fi
    PARTS_JSON+="{\"PartNumber\": $PART_NUM, \"ETag\": $ETAG}"
    
    OFFSET=$((OFFSET + THIS_CHUNK_SIZE))
    PART_NUM=$((PART_NUM + 1))
done

PARTS_JSON+="]"

# Step 3: 完成上传
echo "[INFO] Step 3: 完成上传..."
COMPLETE_RESPONSE=$(curl -s -X POST "${R2_UPLOADER_URL}/api/upload/complete" \
    -H "Content-Type: application/json" \
    -d "{\"key\": \"$KEY\", \"uploadId\": \"$UPLOAD_ID\", \"parts\": $PARTS_JSON}")

SUCCESS=$(echo "$COMPLETE_RESPONSE" | grep -o '"success":true')

if [ -z "$SUCCESS" ]; then
    echo "[ERROR] 完成上传失败: $COMPLETE_RESPONSE"
    exit 1
fi

# 生成下载 URL
DOWNLOAD_URL="https://assets.cnaron.com/$KEY"

echo "[SUCCESS] 上传完成!"
echo "[INFO] 下载链接: $DOWNLOAD_URL"

# 输出下载 URL 供其他脚本使用
echo "$DOWNLOAD_URL" > /tmp/last_r2_url.txt
