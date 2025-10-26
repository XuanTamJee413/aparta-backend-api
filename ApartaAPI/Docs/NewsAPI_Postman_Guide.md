# 📮 News API - Postman Testing Guide

## 🔧 Setup

### 1. Base URL
```
http://localhost:5000
```

### 2. Authentication
Tất cả API cần JWT Token:
- Header: `Authorization: Bearer YOUR_TOKEN`
- Lấy token từ API Login trước

---

## 📋 API Endpoints

### 1️⃣ **GET - Lấy tất cả News**

**Endpoint:** `GET /api/News`

**Headers:**
```
Authorization: Bearer YOUR_TOKEN
```

**Response Success (200):**
```json
{
  "data": [
    {
      "newsId": "abc123",
      "title": "Thông báo",
      "content": "Nội dung...",
      "authorUserId": "user123",
      "authorName": "Nguyễn Văn A",
      "status": "draft",
      "publishedDate": "2024-01-15T10:00:00Z",
      "createdAt": "2024-01-15T09:00:00Z",
      "updatedAt": "2024-01-15T09:00:00Z"
    }
  ],
  "succeeded": true,
  "message": null
}
```

---

### 2️⃣ **GET - Search News**

**Endpoint:** `GET /api/News?searchTerm=thông báo`

**Query Parameters:**
- `searchTerm` (optional): Tìm trong Title hoặc Content

**Example:**
```
GET /api/News?searchTerm=bảo trì
```

---

### 3️⃣ **POST - Tạo News mới**

**Endpoint:** `POST /api/News`

**Headers:**
```
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json
```

**Body:**
```json
{
  "title": "Thông báo bảo trì hệ thống",
  "content": "Hệ thống sẽ bảo trì vào ngày 01/01/2024 từ 2h-4h sáng.",
  "publishedDate": "2024-01-01T02:00:00Z"
}
```

**Notes:**
- `title`: Bắt buộc, max 255 ký tự
- `content`: Bắt buộc, không giới hạn
- `publishedDate`: Optional, mặc định = UTC Now
- `status`: Auto set = "draft"

**Response Success (201):**
```json
{
  "data": {
    "newsId": "abc123...",
    "title": "Thông báo bảo trì hệ thống",
    "content": "Hệ thống sẽ bảo trì...",
    "authorUserId": "user123",
    "authorName": "Nguyễn Văn A",
    "status": "draft",
    "publishedDate": "2024-01-01T02:00:00Z",
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-01-15T10:30:00Z"
  },
  "succeeded": true,
  "message": "SM04"
}
```

**Response Error - Validation (400):**
```json
{
  "data": null,
  "succeeded": false,
  "message": "Title là bắt buộc"
}
```

---

### 4️⃣ **PUT - Update News**

**Endpoint:** `PUT /api/News/{newsId}`

**Headers:**
```
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json
```

**Body (tất cả fields optional):**
```json
{
  "title": "Thông báo đã cập nhật",
  "content": "Nội dung mới",
  "status": "active",
  "publishedDate": "2024-02-01T10:00:00Z"
}
```

**Example - Chỉ update Status:**
```json
{
  "status": "active"
}
```

**Status values:**
- `"draft"` - Nháp
- `"active"` - Đã duyệt/xuất bản
- `"delete"` - Đã xóa

**Response Success (200):**
```json
{
  "data": {
    "newsId": "abc123",
    "title": "Thông báo đã cập nhật",
    "content": "Nội dung mới",
    "status": "active",
    ...
  },
  "succeeded": true,
  "message": "SM03"
}
```

**Response Error - Not Found (404):**
```json
{
  "data": null,
  "succeeded": false,
  "message": "SM01"
}
```

---

### 5️⃣ **DELETE - Xóa News (Soft Delete)**

**Endpoint:** `DELETE /api/News/{newsId}`

**Headers:**
```
Authorization: Bearer YOUR_TOKEN
```

**Notes:**
- Không xóa khỏi database
- Chỉ set `status = "delete"`

**Response Success (200):**
```json
{
  "data": null,
  "succeeded": true,
  "message": "SM05"
}
```

**Response Error - Not Found (404):**
```json
{
  "data": null,
  "succeeded": false,
  "message": "SM01"
}
```

---

## 🧪 Test Cases

### ✅ Test Case 1: Tạo News thành công
1. POST `/api/News`
2. Body hợp lệ
3. **Expected:** Status 201, `status = "draft"`

### ✅ Test Case 2: Validation - Title quá dài
1. POST `/api/News`
2. Title > 255 ký tự
3. **Expected:** Status 400, message validation error

### ✅ Test Case 3: Validation - Thiếu Title
1. POST `/api/News`
2. Body không có `title`
3. **Expected:** Status 400, "Title là bắt buộc"

### ✅ Test Case 4: Validation - Thiếu Content
1. POST `/api/News`
2. Body không có `content`
3. **Expected:** Status 400, "Content là bắt buộc"

### ✅ Test Case 5: Update Status (Draft → Active)
1. POST tạo news mới (status = "draft")
2. PUT `/api/News/{id}` với `{ "status": "active" }`
3. **Expected:** Status 200, status đổi thành "active"

### ✅ Test Case 6: Soft Delete
1. DELETE `/api/News/{id}`
2. GET `/api/News` → News vẫn xuất hiện
3. **Expected:** News có `status = "delete"`

### ✅ Test Case 7: Search News
1. GET `/api/News?searchTerm=bảo trì`
2. **Expected:** Trả về news có "bảo trì" trong title hoặc content

### ✅ Test Case 8: Update không tồn tại
1. PUT `/api/News/INVALID_ID`
2. **Expected:** Status 404, message "SM01"

---

## 🔄 Workflow Example

```
1. Tạo News
   POST /api/News
   → status = "draft"

2. Duyệt News
   PUT /api/News/{id}
   { "status": "active" }
   
3. Xem News đã duyệt
   GET /api/News
   → Filter client-side: status === "active"

4. Xóa News
   DELETE /api/News/{id}
   → status = "delete"
```

---

## 🎯 Message Codes

| Code | Meaning |
|------|---------|
| SM01 | No results / Not found |
| SM03 | Update success |
| SM04 | Create success |
| SM05 | Delete success |
| SM08 | Exceeded max length |
| SM13 | Account does not exist |

---

## 🚀 Quick Start - Postman

1. **Import vào Postman:**
   - File → Import → `NewsAPI.http`

2. **Set Environment Variables:**
   ```
   baseUrl: http://localhost:5000
   token: [YOUR_JWT_TOKEN]
   newsId: [NEWS_ID_FROM_CREATE]
   ```

3. **Test theo thứ tự:**
   1. Login để lấy token
   2. POST tạo news → Copy newsId
   3. GET xem danh sách
   4. PUT update news
   5. DELETE xóa news

