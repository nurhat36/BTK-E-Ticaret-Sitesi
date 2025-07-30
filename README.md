# E-Ticaret Sitesi Projesi

Bu proje, ASP.NET Core MVC kullanılarak geliştirilmiş tam işlevsel bir e-ticaret platformudur. Kullanıcılar ürünleri görüntüleyebilir, sepetlerine ekleyebilir, sipariş verebilir ve yöneticiler ürün ve kategori yönetimi yapabilir.

## 🚀 Proje Özellikleri

### 👤 Kullanıcı Tarafı
- Kullanıcı kaydı ve giriş sistemi
- Ürünleri listeleme ve detay sayfası
- Sepete ürün ekleme / sepeti görüntüleme
- Sipariş oluşturma
- Profil sayfası ve sipariş geçmişi
- Ürün yorumları ve puanlama sistemi

### 🛠️ Yönetici Paneli
- Ürün ve kategori yönetimi
- Siparişleri görüntüleme
- Kullanıcı yönetimi
- İstatistik ekranı (grafikler ve analizler)
- Dashboard: Haftalık sipariş analizi, en çok satan ürünler

### 📦 Veritabanı Yapısı
- `Users` – Kullanıcı bilgileri
- `Products` – Ürün bilgileri
- `Categories` – Ürün kategorileri
- `Orders` – Siparişler
- `OrderDetails` – Siparişe ait ürün detayları
- `Reviews` – Kullanıcı yorumları
- `StoreStatsViewModel` – Yönetim paneli için analiz verileri

## 🛠️ Kullanılan Teknolojiler

- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- Identity (Kullanıcı Kimlik Doğrulama)
- Bootstrap & jQuery (Frontend)
- Chart.js (İstatistik grafikler)

## 🔧 Kurulum Talimatları

1. Bu repoyu klonlayın:
   ```bash
   git clone https://github.com/kullanici/e-ticaret-projesi.git
