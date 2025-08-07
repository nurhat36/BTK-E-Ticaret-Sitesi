# 🛒 AI Destekli E-Ticaret Web Sitesi

Bu proje, .NET MVC framework'ü ile geliştirilmiş bir **e-ticaret web uygulamasıdır**. Hackathon kapsamında geliştirilen bu sistem, geleneksel e-ticaret deneyimini **Yapay Zeka (AI)** teknolojileri ile zenginleştirerek kullanıcı dostu, akıllı ve etkileşimli bir alışveriş deneyimi sunmayı hedefler.

## 🚀 Özellikler

### 🔹 1. AI Chatbot (RAG Tabanlı)
Kullanıcılar, aradıkları ürün hakkında sorular sorabilir ve aşağıdaki konularda **doğrudan yanıt alabilir**:
- Ürün özellikleri  
- Stok durumu  
- Kargo ve iade politikaları  
- Site kullanım şartları  

Chatbot, **Retrieval-Augmented Generation (RAG)** teknolojisini kullanır. Kullanıcının sorusu, ürün veri tabanıyla ilişkilendirilir ve ilgili içerik AI modeli ile sentezlenerek yanıtlanır.

### 🔹 2. Ürün Açıklama Metni Oluşturucu (AI Text Generator)
Ürün yöneticileri, yalnızca başlık ve kısa bilgi girerek detaylı ürün açıklamaları oluşturabilir. Açıklamalar, kullanıcı dostu ve SEO uyumlu olarak yapay zeka tarafından hazırlanır.

### 🔹 3. Yorum Analizi ve Kalite Skoru
Her ürün için kullanıcı yorumları periyodik olarak analiz edilir:
- **30 yorumda bir**, yorumlar AI'a gönderilir.
- AI, ürüne dair genel memnuniyet seviyesini belirler ve bir **kalite puanı** verir.
- Kullanıcıların en çok neyi olumlu/olumsuz bulduğu görselleştirilir.

### 🔹 4. Soru-Cevap Analizi
Kullanıcılar tarafından ürünlere sorulan sorular ve verilen cevaplar analiz edilerek:
- En çok sorulan konular
- Sık tekrar eden sorunlar ya da merak edilenler
- Sorulara verilen yanıtların kalitesi

Yapay zeka bu verileri işleyerek içgörü sunar.

---

## ⚙️ Kurulum ve Çalıştırma

### 🔧 Gereksinimler
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- [Visual Studio 2022+](https://visualstudio.microsoft.com/)
- [SQL Server 2019+](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- Google Generative AI API Key (Chatbot ve AI açıklama üretimi için)

---

### 🛠️ Adımlar

#### 1. Projeyi Klonlayın
```bash
git clone https://github.com/kullaniciAdi/ai-ecommerce.git
cd ai-ecommerce
```

#### 2. Bağımlılıkları Giderin
Visual Studio ile projeyi açın ve restore işlemini gerçekleştirin.

#### 3. `appsettings.Development.json` Ayarları
Aşağıdaki gibi bir yapı örnek dosyada tanımlıdır, doldurulması gerekir:

```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    },
    "AllowedHosts": "*",
    "ConnectionStrings": {
        "DefaultConnection": "Server=.;Database=***********;Trusted_Connection=True;TrustServerCertificate=true;",
        "AppDbContextConnection": "Server=(localdb)\\mssqllocaldb;Database=*********;Trusted_Connection=True;MultipleActiveResultSets=true"
    },
    "SmtpSettings": {
        "Host": "smtp.gmail.com",
        "Port": 587,
        "EnableSsl": true,
        "SenderName": "ETicaret Sitesi",
        "SenderEmail": "**************",
        "UserName": "********************",
        "Password": "***************************"//buraya google uygulama şifrenizi girin
    },
    "IyzicoOptions": {
        "ApiKey": "sandbox-***********************",
        "SecretKey": "sandbox-*******************************",
        "BaseUrl": "https://sandbox-api.iyzipay.com"
    }
}
```
#### user secret kısmınada bu yapı yazılmalı
```json
{
    "GoogleAI": {
        "ApiKey": "****************************"
    }
}
'''

#### 4. Kullanıcı Secret Ekleme (Alternatif)
```bash
dotnet user-secrets set "GoogleGenerativeAI:ApiKey" "YOUR_API_KEY"
```

#### 5. Veritabanını Oluşturma
```bash
dotnet ef database update
```

#### 6. Uygulamayı Başlatma
```bash
dotnet run
```

---

## 🧠 Kullanılan Teknolojiler

| Teknoloji           | Açıklama |
|---------------------|----------|
| .NET 8 MVC          | Web uygulama çatısı |
| Entity Framework    | ORM (Veritabanı işlemleri) |
| MSSQL Server        | Veritabanı |
| Google Generative AI | Chatbot, metin üretimi ve analiz için |
| Bootstrap / jQuery  | Arayüz düzeni ve dinamik özellikler |
| RAG (Retrieval-Augmented Generation) | Chatbot mimarisi |

---

## siteye kayıt olurken kayıt olduğunuz e posta adresine ber e mail gelir maildeki linlke tıklarsanız hesabınız doğrulanır ve artık giriş yapabilirsiniz


## 📂 Proje Yapısı

```bash


---

## 💡 Katkı ve Geliştirme
Proje hackathon için geliştirilmiş olsa da genişletilmeye ve topluluk katkılarına açıktır. Öneriler, pull request'ler ve hata bildirimleri memnuniyetle karşılanır.

---

## 📜 Lisans
Bu proje, Hackathon sunumu kapsamında geliştirilmiş olup kişisel/tanıtım amaçlıdır.
