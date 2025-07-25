using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using BTKETicaretSitesi.Models;

namespace BTKETicaretSitesi.Data
{
    public static class SeedData
    {
        public static async Task Initialize(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            // Mevcut rolleri oluştur
            string[] roles = { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Admin kullanıcı oluştur
            var adminEmail = "admin@site.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Slug = "admin",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    PhoneNumber = "5551234567",
                    ProfileImageUrl = "/uploads/profile-images/default.jpg"
                };
                await userManager.CreateAsync(adminUser, "Admin123!");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // Kategorileri ekle (eğer hiç yoksa)
            if (!context.Categories.Any())
            {
                var mainCategories = new List<Category>
    {
        new Category { Name = "Elektronik", Description = "Elektronik ürünler", Slug = "elektronik", ImageUrl = "bi bi-phone" },
        new Category { Name = "Moda", Description = "Giyim, ayakkabı ve aksesuar", Slug = "moda", ImageUrl = "bi bi-shirt" },
        new Category { Name = "Ev & Yaşam", Description = "Mobilya, dekorasyon", Slug = "ev-yasam", ImageUrl = "bi bi-house" },
        new Category { Name = "Kozmetik", Description = "Kişisel bakım", Slug = "kozmetik", ImageUrl = "bi bi-heart" },
        new Category { Name = "Anne & Bebek", Description = "Bebek ürünleri", Slug = "anne-bebek", ImageUrl = "bi bi-baby" },
        new Category { Name = "Spor & Outdoor", Description = "Spor ekipmanları", Slug = "spor-outdoor", ImageUrl = "bi bi-bicycle" },
        new Category { Name = "Kitap & Kırtasiye", Description = "Eğitim, kitap, ofis", Slug = "kitap-kirtasiye", ImageUrl = "bi bi-book" },
        new Category { Name = "Oyun & Hobi", Description = "Oyun konsolları ve hobi ürünleri", Slug = "oyun-hobi", ImageUrl = "bi bi-controller" },
        new Category { Name = "Süpermarket", Description = "Gıda ve temizlik ürünleri", Slug = "supermarket", ImageUrl = "bi bi-cart" },
        new Category { Name = "Otomotiv", Description = "Araç parçaları ve aksesuarlar", Slug = "otomotiv", ImageUrl = "bi bi-car-front" },
        new Category { Name = "Müzik & Enstrüman", Description = "Müzik aletleri ve ekipmanları", Slug = "muzik-enstruman", ImageUrl = "bi bi-music-note-beamed" },
        new Category { Name = "Film & Dizi", Description = "DVD, Blu-ray ve koleksiyon ürünleri", Slug = "film-dizi", ImageUrl = "bi bi-film" },
        new Category { Name = "Bilgisayar Parçaları", Description = "Donanım ve bileşenler", Slug = "bilgisayar-parcalari", ImageUrl = "bi bi-pc-display" },
        new Category { Name = "Ofis & Ekipman", Description = "Ofis mobilyaları ve malzemeleri", Slug = "ofis-ekipman", ImageUrl = "bi bi-printer" },
        new Category { Name = "Bahçe & Yapı Market", Description = "Bahçe ve yapı malzemeleri", Slug = "bahce-yapi-market", ImageUrl = "bi bi-flower1" },
    };

                await context.Categories.AddRangeAsync(mainCategories);
                await context.SaveChangesAsync();

                // Alt kategoriler (ParentCategoryId eşleştirerek)
                var elektronikId = mainCategories.First(c => c.Slug == "elektronik").Id;
                var modaId = mainCategories.First(c => c.Slug == "moda").Id;
                var evYasamId = mainCategories.First(c => c.Slug == "ev-yasam").Id;
                var kozmetikId = mainCategories.First(c => c.Slug == "kozmetik").Id;
                var anneBebekId = mainCategories.First(c => c.Slug == "anne-bebek").Id;
                var sporOutdoorId = mainCategories.First(c => c.Slug == "spor-outdoor").Id;
                var kitapKirtasiyeId = mainCategories.First(c => c.Slug == "kitap-kirtasiye").Id;
                var oyunHobiId = mainCategories.First(c => c.Slug == "oyun-hobi").Id;
                var supermarketId = mainCategories.First(c => c.Slug == "supermarket").Id;
                var otomotivId = mainCategories.First(c => c.Slug == "otomotiv").Id;
                var muzikEnstrumanId = mainCategories.First(c => c.Slug == "muzik-enstruman").Id;
                var filmDiziId = mainCategories.First(c => c.Slug == "film-dizi").Id;
                var bilgisayarParcalariId = mainCategories.First(c => c.Slug == "bilgisayar-parcalari").Id;
                var ofisEkipmanId = mainCategories.First(c => c.Slug == "ofis-ekipman").Id;
                var bahceYapiMarketId = mainCategories.First(c => c.Slug == "bahce-yapi-market").Id;

                var subCategories = new List<Category>
    {
        // Elektronik Alt Kategorileri
        new Category { Name = "Telefonlar", Description = "Akıllı telefonlar", Slug = "telefonlar", ImageUrl = "bi bi-phone", ParentCategoryId = elektronikId },
        new Category { Name = "Bilgisayarlar", Description = "Laptop, masaüstü", Slug = "bilgisayarlar", ImageUrl = "bi bi-laptop", ParentCategoryId = elektronikId },
        new Category { Name = "Tabletler", Description = "Android ve iPad", Slug = "tabletler", ImageUrl = "bi bi-tablet", ParentCategoryId = elektronikId },
        new Category { Name = "Televizyonlar", Description = "Smart TV ve LED TV", Slug = "televizyonlar", ImageUrl = "bi bi-tv", ParentCategoryId = elektronikId },
        new Category { Name = "Kulaklıklar", Description = "Kablolu ve kablosuz kulaklıklar", Slug = "kulakliklar", ImageUrl = "bi bi-headphones", ParentCategoryId = elektronikId },
        new Category { Name = "Fotoğraf & Kamera", Description = "DSLR, aksiyon kameraları", Slug = "fotograf-kamera", ImageUrl = "bi bi-camera", ParentCategoryId = elektronikId },
        
        // Moda Alt Kategorileri
        new Category { Name = "Kadın Giyim", Description = "Elbise, bluz", Slug = "kadin-giyim", ImageUrl = "bi bi-person-fill", ParentCategoryId = modaId },
        new Category { Name = "Erkek Giyim", Description = "Tişört, pantolon", Slug = "erkek-giyim", ImageUrl = "bi bi-person", ParentCategoryId = modaId },
        new Category { Name = "Ayakkabı", Description = "Kadın ve erkek ayakkabılar", Slug = "ayakkabi", ImageUrl = "bi bi-shoe-print", ParentCategoryId = modaId },
        new Category { Name = "Çanta", Description = "Sırt çantası, el çantası", Slug = "canta", ImageUrl = "bi bi-bag", ParentCategoryId = modaId },
        new Category { Name = "Takı & Saat", Description = "Kolye, bileklik, saat", Slug = "taki-saat", ImageUrl = "bi bi-watch", ParentCategoryId = modaId },
        new Category { Name = "Gözlük", Description = "Güneş gözlüğü, optik gözlük", Slug = "gozluk", ImageUrl = "bi bi-eyeglasses", ParentCategoryId = modaId },
        
        // Ev & Yaşam Alt Kategorileri
        new Category { Name = "Mobilya", Description = "Kanepe, yatak, dolap", Slug = "mobilya", ImageUrl = "bi bi-house-door", ParentCategoryId = evYasamId },
        new Category { Name = "Mutfak Eşyaları", Description = "Tencere, tava, mutfak gereçleri", Slug = "mutfak-esyalari", ImageUrl = "bi bi-egg-fried", ParentCategoryId = evYasamId },
        new Category { Name = "Ev Tekstili", Description = "Yatak örtüsü, perde", Slug = "ev-tekstili", ImageUrl = "bi bi-border-style", ParentCategoryId = evYasamId },
        new Category { Name = "Aydınlatma", Description = "Lamba, avize", Slug = "aydinlatma", ImageUrl = "bi bi-lightbulb", ParentCategoryId = evYasamId },
        new Category { Name = "Dekorasyon", Description = "Tablo, vazo", Slug = "dekorasyon", ImageUrl = "bi bi-flower2", ParentCategoryId = evYasamId },
        
        // Kozmetik Alt Kategorileri
        new Category { Name = "Parfüm", Description = "Kadın ve erkek parfümleri", Slug = "parfum", ImageUrl = "bi bi-droplet", ParentCategoryId = kozmetikId },
        new Category { Name = "Makyaj", Description = "Ruj, fondöten, far", Slug = "makyaj", ImageUrl = "bi bi-palette", ParentCategoryId = kozmetikId },
        new Category { Name = "Cilt Bakımı", Description = "Nemlendirici, temizleyici", Slug = "cilt-bakimi", ImageUrl = "bi bi-moisture", ParentCategoryId = kozmetikId },
        new Category { Name = "Saç Bakımı", Description = "Şampuan, saç kremi", Slug = "sac-bakimi", ImageUrl = "bi bi-scissors", ParentCategoryId = kozmetikId },
        
        // Anne & Bebek Alt Kategorileri
        new Category { Name = "Bebek Bezi", Description = "Islak mendil, bebek bezi", Slug = "bebek-bezi", ImageUrl = "bi bi-baby", ParentCategoryId = anneBebekId },
        new Category { Name = "Bebek Giyim", Description = "Bebek kıyafetleri", Slug = "bebek-giyim", ImageUrl = "bi bi-tshirt", ParentCategoryId = anneBebekId },
        new Category { Name = "Hamile Giyim", Description = "Hamile kıyafetleri", Slug = "hamile-giyim", ImageUrl = "bi bi-person-heart", ParentCategoryId = anneBebekId },
        new Category { Name = "Bebek Odası", Description = "Bebek karyolası, bebek arabası", Slug = "bebek-odasi", ImageUrl = "bi bi-house-add", ParentCategoryId = anneBebekId },
        
        // Spor & Outdoor Alt Kategorileri
        new Category { Name = "Fitness", Description = "Dambıl, koşu bandı", Slug = "fitness", ImageUrl = "bi bi-activity", ParentCategoryId = sporOutdoorId },
        new Category { Name = "Outdoor", Description = "Çadır, uyku tulumu", Slug = "outdoor", ImageUrl = "bi bi-tree", ParentCategoryId = sporOutdoorId },
        new Category { Name = "Bisiklet", Description = "Dağ bisikleti, şehir bisikleti", Slug = "bisiklet", ImageUrl = "bi bi-bicycle", ParentCategoryId = sporOutdoorId },
        new Category { Name = "Spor Ayakkabı", Description = "Koşu ayakkabısı, spor ayakkabı", Slug = "spor-ayakkabi", ImageUrl = "bi bi-shoe-print", ParentCategoryId = sporOutdoorId },
        
        // Kitap & Kırtasiye Alt Kategorileri
        new Category { Name = "Roman", Description = "Yerli ve yabancı romanlar", Slug = "roman", ImageUrl = "bi bi-book", ParentCategoryId = kitapKirtasiyeId },
        new Category { Name = "Kişisel Gelişim", Description = "Kişisel gelişim kitapları", Slug = "kisisel-gelisim", ImageUrl = "bi bi-journal-bookmark", ParentCategoryId = kitapKirtasiyeId },
        new Category { Name = "Defter & Ajanda", Description = "Defter, ajanda, not defteri", Slug = "defter-ajanda", ImageUrl = "bi bi-journal-text", ParentCategoryId = kitapKirtasiyeId },
        new Category { Name = "Kırtasiye Malzemeleri", Description = "Kalem, silgi, cetvel", Slug = "kirtasiye-malzemeleri", ImageUrl = "bi bi-pencil", ParentCategoryId = kitapKirtasiyeId },
        
        // Oyun & Hobi Alt Kategorileri
        new Category { Name = "Oyun Konsolları", Description = "PlayStation, Xbox", Slug = "oyun-konsollari", ImageUrl = "bi bi-controller", ParentCategoryId = oyunHobiId },
        new Category { Name = "Bilgisayar Oyunları", Description = "PC oyunları", Slug = "bilgisayar-oyunlari", ImageUrl = "bi bi-joystick", ParentCategoryId = oyunHobiId },
        new Category { Name = "Yapboz", Description = "Puzzle, yapboz", Slug = "yapboz", ImageUrl = "bi bi-puzzle", ParentCategoryId = oyunHobiId },
        new Category { Name = "Model Uçak", Description = "Hobi ürünleri", Slug = "model-ucak", ImageUrl = "bi bi-airplane", ParentCategoryId = oyunHobiId },
        
        // Süpermarket Alt Kategorileri
        new Category { Name = "Atıştırmalık", Description = "Cips, çikolata", Slug = "atistirmalik", ImageUrl = "bi bi-cup-straw", ParentCategoryId = supermarketId },
        new Category { Name = "İçecek", Description = "Su, meyve suyu", Slug = "icecek", ImageUrl = "bi bi-cup-hot", ParentCategoryId = supermarketId },
        new Category { Name = "Temizlik Ürünleri", Description = "Deterjan, sabun", Slug = "temizlik-urunleri", ImageUrl = "bi bi-bucket", ParentCategoryId = supermarketId },
        new Category { Name = "Kahvaltılık", Description = "Peynir, zeytin", Slug = "kahvaltilik", ImageUrl = "bi bi-egg-fried", ParentCategoryId = supermarketId },
        
        // Otomotiv Alt Kategorileri
        new Category { Name = "Araç Bakım", Description = "Motor yağı, antifriz", Slug = "arac-bakim", ImageUrl = "bi bi-tools", ParentCategoryId = otomotivId },
        new Category { Name = "Araç İçi Aksesuar", Description = "Koltuk kılıfı, paspas", Slug = "arac-ici-aksesuar", ImageUrl = "bi bi-car-front-fill", ParentCategoryId = otomotivId },
        new Category { Name = "Oto Elektronik", Description = "Araç şarj aleti, kamera", Slug = "oto-elektronik", ImageUrl = "bi bi-ev-station", ParentCategoryId = otomotivId },
        new Category { Name = "Lastik & Jant", Description = "Yaz lastiği, kış lastiği", Slug = "lastik-jant", ImageUrl = "bi bi-circle", ParentCategoryId = otomotivId },
        
        // Müzik & Enstrüman Alt Kategorileri
        new Category { Name = "Gitar", Description = "Akustik gitar, elektro gitar", Slug = "gitar", ImageUrl = "bi bi-music-note-list", ParentCategoryId = muzikEnstrumanId },
        new Category { Name = "Piyano", Description = "Dijital piyano", Slug = "piyano", ImageUrl = "bi bi-music-note-beamed", ParentCategoryId = muzikEnstrumanId },
        new Category { Name = "Davul", Description = "Akustik davul, elektronik davul", Slug = "davul", ImageUrl = "bi bi-drum", ParentCategoryId = muzikEnstrumanId },
        new Category { Name = "Mikrofon", Description = "Stüdyo mikrofonu", Slug = "mikrofon", ImageUrl = "bi bi-mic", ParentCategoryId = muzikEnstrumanId },
        
        // Film & Dizi Alt Kategorileri
        new Category { Name = "DVD", Description = "Film DVD'leri", Slug = "dvd", ImageUrl = "bi bi-disc", ParentCategoryId = filmDiziId },
        new Category { Name = "Blu-ray", Description = "Blu-ray filmler", Slug = "blu-ray", ImageUrl = "bi bi-disc-fill", ParentCategoryId = filmDiziId },
        new Category { Name = "Film Koleksiyon", Description = "Özel koleksiyon ürünleri", Slug = "film-koleksiyon", ImageUrl = "bi bi-collection", ParentCategoryId = filmDiziId },
        new Category { Name = "Dizi Box Set", Description = "Dizi sezonları", Slug = "dizi-box-set", ImageUrl = "bi bi-collection-play", ParentCategoryId = filmDiziId },
        
        // Bilgisayar Parçaları Alt Kategorileri
        new Category { Name = "İşlemci", Description = "CPU, işlemci", Slug = "islemci", ImageUrl = "bi bi-cpu", ParentCategoryId = bilgisayarParcalariId },
        new Category { Name = "Ekran Kartı", Description = "NVIDIA, AMD ekran kartları", Slug = "ekran-karti", ImageUrl = "bi bi-gpu-card", ParentCategoryId = bilgisayarParcalariId },
        new Category { Name = "RAM", Description = "DDR4, DDR5 RAM", Slug = "ram", ImageUrl = "bi bi-memory", ParentCategoryId = bilgisayarParcalariId },
        new Category { Name = "Anakart", Description = "Intel, AMD anakartlar", Slug = "anakart", ImageUrl = "bi bi-motherboard", ParentCategoryId = bilgisayarParcalariId },
        
        // Ofis & Ekipman Alt Kategorileri
        new Category { Name = "Yazıcı", Description = "Lazer yazıcı, mürekkep püskürtmeli", Slug = "yazici", ImageUrl = "bi bi-printer", ParentCategoryId = ofisEkipmanId },
        new Category { Name = "Projeksiyon", Description = "Projeksiyon cihazı", Slug = "projeksiyon", ImageUrl = "bi bi-projector", ParentCategoryId = ofisEkipmanId },
        new Category { Name = "Ofis Mobilyası", Description = "Masa, sandalye", Slug = "ofis-mobilyasi", ImageUrl = "bi bi-chair", ParentCategoryId = ofisEkipmanId },
        new Category { Name = "Toplantı Ekipmanları", Description = "Flipchart, beyaz tahta", Slug = "toplanti-ekipmanlari", ImageUrl = "bi bi-easel", ParentCategoryId = ofisEkipmanId },
        
        // Bahçe & Yapı Market Alt Kategorileri
        new Category { Name = "Bahçe Mobilyası", Description = "Bahçe masası, şezlong", Slug = "bahce-mobilyasi", ImageUrl = "bi bi-table", ParentCategoryId = bahceYapiMarketId },
        new Category { Name = "Bahçe Aletleri", Description = "Tırmık, kürek", Slug = "bahce-aletleri", ImageUrl = "bi bi-tools", ParentCategoryId = bahceYapiMarketId },
        new Category { Name = "Yapı Malzemeleri", Description = "Çimento, boya", Slug = "yapi-malzemeleri", ImageUrl = "bi bi-bricks", ParentCategoryId = bahceYapiMarketId },
        new Category { Name = "Aydınlatma", Description = "Bahçe lambası, solar lamba", Slug = "bahce-aydinlatma", ImageUrl = "bi bi-lightbulb", ParentCategoryId = bahceYapiMarketId },
    };

                await context.Categories.AddRangeAsync(subCategories);
                await context.SaveChangesAsync();
            }
        }

    }
}
