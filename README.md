# ⚡ Caching Solution – High-Performance Caching in .NET 9 🚀  

![.NET 9](https://img.shields.io/badge/.NET%209-blue?style=for-the-badge)
![Redis](https://img.shields.io/badge/Redis-%E2%9D%A4-red?style=for-the-badge)
![Performance Boost](https://img.shields.io/badge/Performance%20Boost-%E2%9C%85-green?style=for-the-badge)
![Scalability](https://img.shields.io/badge/Scalability-%F0%9F%9A%80-purple?style=for-the-badge)
![Memory Caching](https://img.shields.io/badge/Memory%20Caching-%F0%9F%92%AB-orange?style=for-the-badge)

## 🎯 Overview  

This **.NET 9 Caching Solution** demonstrates multiple caching techniques using **ASP.NET Core**, including **In-Memory Caching, Distributed Caching (Redis), Output Caching, and Response Caching**. The goal is to **optimize API performance, reduce database load, and enhance scalability** for modern applications.  

> **Why Caching?**  
> - 🚀 **Speed Up Responses** – Reduce API response time with fast data retrieval.  
> - 🛠 **Reduce Server Load** – Minimize unnecessary database queries.  
> - 📈 **Scalability** – Handle high-traffic applications efficiently.  

---

## 🌟 Features  

✅ **In-Memory Caching** – Fast, efficient caching within the application.  
✅ **Distributed Caching (Redis)** – Scalable caching solution for cloud and large-scale apps.  
✅ **Output Caching** – Caches entire HTTP responses to boost performance.  
✅ **Response Caching** – Adds caching headers for optimized client-side caching.  
✅ **Advanced Caching Strategies**:  
   - 🔹 **Sliding Expiration** – Keeps cache alive if frequently accessed.  
   - 🔹 **Post-Eviction Callbacks** – Logs when cache entries expire.  
   - 🔹 **Cache Dependencies** – Invalidates cache when related data changes.  

---

## 🏗️ Architecture & Project Structure  

📌 **src/Caching.Api** – Main API project with caching endpoints.  
📌 **src/Caching.Api/Services** – Business logic for caching and product retrieval.  
📌 **src/Caching.Api/Endpoints** – Defines API routes for caching functionalities.  

### 🏛️ **Implemented Caching Techniques**  

🔹 **Memory Cache** – Stores frequently accessed data in-memory.  
🔹 **Redis Cache** – Provides distributed caching across multiple servers.  
🔹 **Output Cache** – Caches HTTP responses at the server level.  
🔹 **Response Cache** – Implements client-side cache control headers.  

---

## 🚀 Getting Started  

### **📌 Prerequisites**  
✅ [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)  
✅ [Redis](https://redis.io/download) (for distributed caching)  
✅ [Docker](https://www.docker.com/) (optional for Redis container)  

### **Step 1: Clone the Repository**  
```bash
git clone https://github.com/yourusername/caching-solution.git
cd caching-solution
```

### **Step 2: Install Dependencies**  
```bash
dotnet restore
```

### **Step 3: Configure Redis (Optional, for Distributed Caching)**  
Update `appsettings.json`:  
```json
{
  "ConnectionStrings": {
    "Redis": "your_redis_connection_string"
  }
}
```

### **Step 4: Run Redis with Docker (Optional)**  
```bash
docker run -d -p 6379:6379 redis
```

### **Step 5: Run the Application**  
```bash
dotnet run --project src/Caching.Api
```

---

## 🌍 API Endpoints  

🔹 **Basic Product Retrieval**  
| Method | Endpoint               | Description |
|--------|------------------------|-------------|
| **GET** | `/products`            | Fetches products **without caching** |

🔹 **Memory Caching**  
| Method | Endpoint                         | Description |
|--------|----------------------------------|-------------|
| **GET** | `/absolute-cache-products`       | Memory cache with **absolute expiration** |
| **GET** | `/sliding-cache-products`        | Memory cache with **sliding expiration** |
| **GET** | `/eviction-callback-cache-products` | Cache with **post-eviction callback** |
| **GET** | `/priority-cache-products`       | Cache with **priority handling** |
| **GET** | `/dependent-cache-products`      | Cache with **dependencies** |

🔹 **Distributed Caching (Memory + Redis)**  
| Method | Endpoint                                 | Description |
|--------|----------------------------------------|-------------|
| **GET** | `/memory-redis-cache-products`       | Products with **in-memory + Redis caching** |
| **GET** | `/memory-redis-cache-products-sliding` | Products with **sliding expiration** |
| **GET** | `/memory-redis-cache-products-eviction-callback` | Products with **post-eviction callback** |
| **GET** | `/memory-redis-cache-products-dependency` | Products with **cache dependency** |

🔹 **Output Caching**  
| Method | Endpoint                | Description |
|--------|------------------------|-------------|
| **GET** | `/output-cache-products` | Caches **entire HTTP response** |
| **GET** | `/output-cache`          | Output cache with **timestamp** |

🔹 **Response Caching**  
| Method | Endpoint               | Description |
|--------|-----------------------|-------------|
| **GET** | `/response-cache-products` | Adds **cache control headers** |
| **GET** | `/response-cache`         | Response cache with **vary header** |

---

## 🧪 Testing  

### **Unit Tests**  
Run unit tests for caching and API responses:  
```bash
dotnet test
```

### **Manual API Testing**  
📌 **Use Postman or Swagger UI** to:  
✅ **Test without caching** → `/products`  
✅ **Enable caching** → `/memory-cache-products`  
✅ **Test Redis caching** → `/memory-redis-cache-products`  
✅ **Check output caching** → `/output-cache-products`  

---

## 🎯 Why Use This Project?  

✅ **Blazing-Fast API Responses** – Implements multiple caching strategies.  
✅ **Scalable & Cloud-Ready** – Uses Redis for **distributed caching**.  
✅ **Performance-Oriented** – Reduces unnecessary **database queries**.  
✅ **Flexible & Configurable** – Supports **multiple expiration policies**.  
✅ **Enterprise-Grade Architecture** – Built using **.NET 9** best practices.  

---

## 📜 License  

This project is licensed under the **MIT License**. See [LICENSE](LICENSE) for details.  

---

## 📞 Contact  

For feedback, contributions, or questions:  
📧 **Email**: mreshboboyev@gmail.com  
💻 **GitHub**: [MrEshboboyev](https://github.com/MrEshboboyev)  

---

🚀 **Supercharge your .NET APIs with advanced caching!** Clone the repo & start optimizing today!  
```  
