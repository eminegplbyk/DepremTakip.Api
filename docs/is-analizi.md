# DepremTakip — İş Analizi

| | |
|---|---|
| **Proje** | DepremTakip API |
| **Hazırlayan** | Emine Gül Palabıyık |
| **Sürüm** | 0.2 |
| **Son güncelleme** | 30.10.2026 |

---

## 1. Problem ve Amaç

### 1.1 Problem

Deprem bilgisine AFAD, Kandilli Rasathanesi, mobil uygulamalar ve sosyal medya üzerinden ulaşılabiliyor. Ancak bu kaynaklar çoğunlukla Türkiye genelindeki **son depremlerin anlık listesini** sunuyor:

- Kullanıcı bir depremi gördüğünde "bu ilde son bir ayda kaç deprem oldu?" sorusunun cevabına kolayca ulaşamıyor.
- Kullanıcı sadece kendi bölgesini veya belirli büyüklükteki depremleri takip edemiyor.
- Veriyi rapor veya uygulama için kullanmak isteyenler ham veriyi kendileri toplayıp işlemek zorunda kalıyor.

### 1.2 Amaç

Deprem verisini düzenli olarak toplayan, geçmişe dönük sorgulanabilir şekilde saklayan, kullanıcıya özel uyarılar sunan ve analiz için istatistik üreten bir API geliştirmek.

### 1.3 Kapsam dışı (bu sürüm)

| Konu | Gerekçe |
|---|---|
| E-posta / SMS / push bildirimi | Ek altyapı (e-posta servisi) gerektiriyor. Bu sürümde uyarılar sistemde kayıt olarak tutulacak ve API üzerinden sorgulanabilecek. Gelecek sürümde değerlendirilecek. |
| Mobil uygulama / web arayüzü | Proje yalnızca API'dir; arayüzler bu API'yi kullanarak geliştirilebilir. |
| Erken uyarı | Sistem deprem **sonrası** bilgi ve takip amaçlıdır (bkz. NFR-08). |

## 2. Paydaşlar

| Paydaş | Sistemle ilişkisi | Beklentisi |
|---|---|---|
| **Son kullanıcı** | API'yi kullanan uygulamalar üzerinden veriyi görür | Son depremlerin büyüklük, derinlik, merkez ve saat bilgisini güncel olarak görmek; kendi ilindeki geçmiş depremlere kolayca ulaşmak |
| **Uygulama geliştirici** | API'yi kendi mobil veya web uygulamasında kullanır | Düzenli, belgelenmiş, filtrelenebilir veri |
| **Veri analisti** | İstatistik uç noktalarını ve veritabanını kullanır | Kopya kaydı olmayan, tutarlı, geçmişe dönük veri ile rapor hazırlamak |
| **Kamu kurumları / araştırmacılar** | Raporlardan ve istatistiklerden faydalanır | Bölgesel deprem sıklığı ve dağılım bilgisi |
| **AFAD** | Veri sağlayıcı | Servisine aşırı yük bindirilmemesi |

> Not: Kandilli Rasathanesi, mobil uygulamalar ve sosyal medya **alternatif bilgi kaynaklarıdır**; sistemle doğrudan ilişkileri olmadığı için paydaş olarak sayılmamıştır.

## 3. Fonksiyonel Gereksinimler

| No | Gereksinim | Durum |
|---|---|---|
| FR-01 | Sistem, AFAD'dan deprem bilgilerini alabilmelidir. | ✅ |
| FR-02 | Kullanıcı, AFAD'daki son depremleri veritabanına kaydetmeden önizleyebilmelidir. | ✅ |
| FR-03 | Sistem, AFAD'dan gelen depremleri veritabanına kaydedebilmelidir. | ✅ |
| FR-04 | Aynı deprem veritabanına birden fazla kez kaydedilmemelidir. | ✅ |
| FR-05 | AFAD bir depremin bilgisini değiştirirse, sistem yeni kayıt açmadan mevcut kaydı güncellemelidir. | ✅ |
| FR-06 | Sistem, kullanıcı müdahalesi olmadan belirli aralıklarla AFAD'ı kontrol edip yeni depremleri kaydedebilmelidir. | ✅ |
| FR-07 | Kullanıcı, depremleri il, tarih aralığı ve büyüklüğe göre filtreleyip sıralayabilmelidir. | ⏳ |
| FR-08 | Sistem, bir il için belirli bir dönemdeki deprem sayısı ve en büyük deprem gibi özet bilgileri sunabilmelidir. | ⏳ |
| FR-09 | Kullanıcı, depremleri ve istatistikleri giriş yapmadan (misafir olarak) görüntüleyebilmelidir. | ⏳ |
| FR-10 | Kullanıcı, sisteme kayıt olup giriş yapabilmelidir. | ⏳ |
| FR-11 | Giriş yapan kullanıcı, belirli bir il ve minimum büyüklük için alarm kuralı tanımlayabilmelidir. | ⏳ |
| FR-12 | Kurala uyan bir deprem kaydedildiğinde sistem kullanıcı için bir bildirim kaydı oluşturmalı, kullanıcı da bildirimlerini görüntüleyebilmelidir. | ⏳ |

> **FR-08 için kullanım örneği:** Bir uygulama "Malatya'da 4.2 büyüklüğünde deprem" bilgisini gösterirken yanına "Malatya'da son 1 ayın depremleri" butonu koyabilir. Bu butonun arkasındaki veriyi API sağlar.

> **FR-09 gerekçesi:** Vatandaşlar deprem bilgisine ulaşmak için kayıt / giriş işlemiyle uğraşmak istemez. Giriş yalnızca kişiye özel özellikler (alarm kuralları) için gereklidir.

## 4. Fonksiyonel Olmayan Gereksinimler

| No | Kategori | Gereksinim |
|---|---|---|
| NFR-01 | Güncellik | Yeni bir deprem, AFAD'da yayımlandıktan sonra en geç **1 dakika** içinde sisteme kaydedilmelidir. Kontrol aralığı ayarlardan değiştirilebilmelidir. |
| NFR-02 | Doğruluk | Aynı deprem birden fazla kez kaydedilmemelidir. Bu kural hem uygulamada hem veritabanında (unique index) uygulanmalıdır. |
| NFR-03 | Dayanıklılık | AFAD'a ulaşılamadığında sistem çalışmaya devam etmeli, hatayı kaydetmeli ve bir sonraki turda tekrar denemelidir. |
| NFR-04 | Güvenlik | Şifreler veritabanında açık metin olarak değil, hash'lenmiş olarak saklanmalıdır. |
| NFR-05 | Gizlilik | Kullanıcılar yalnızca kendi alarm kurallarını ve bildirimlerini görebilmelidir. Alarm kuralları kullanıcının yaşadığı yeri ele verebileceğinden kişisel veri olarak değerlendirilmeli ve KVKK'ya uygun şekilde işlenmelidir. |
| NFR-06 | Kaynağa saygı | AFAD'a yapılan istekler makul sıklıkta tutulmalı; toplu veri talepleri günlük parçalara bölünmelidir. |
| NFR-07 | Performans | Filtreli liste sorguları kullanıcıya hızlı cevap vermelidir (tarih, büyüklük ve il alanlarında index). |
| NFR-08 | Sorumluluk sınırı | Sistem bir **erken uyarı sistemi değildir**; deprem sonrası bilgi ve takip amaçlıdır. Bu durum kullanıcılara açıkça belirtilmelidir. |

## 5. İş Kuralları

| No | Kural |
|---|---|
| BR-01 | Bir deprem, AFAD'ın verdiği **EventId** ile tanımlanır. Aynı EventId'ye sahip iki kayıt olamaz. |
| BR-02 | Veritabanının kendi `Id` numarası yalnızca iç düzen içindir; depremi tanımak için kullanılmaz. |
| BR-03 | EventId'si boş gelen kayıtlar veritabanına kaydedilmez; geliştiricilerin incelemesi için log'a yazılır. |
| BR-04 | Kayıtlı bir depremin bilgisi AFAD'da değişmişse, AFAD'daki **en güncel bilgi** esas alınır ve kayıt güncellenir. |
| BR-05 | İl bilgisi gelmeyen depremler de kaydedilir; AFAD'ın konum metni olduğu gibi saklanır. |
| BR-06 | Tüm zamanlar UTC olarak saklanır; kullanıcıya Türkiye saatiyle (UTC+3) gösterilir. |
| BR-07 | Alarm kuralı, büyüklük **eşit veya büyük** olduğunda tetiklenir (4.0 kuralı 4.0 depremde çalışır). |
| BR-08 | İl seçilmeyen alarm kuralı Türkiye genelindeki tüm depremler için geçerlidir. |
| BR-09 | Büyüklüğü sonradan güncellenen bir deprem kuralı karşılar hale gelirse bildirim oluşturulur. |
| BR-10 | Aynı deprem için aynı kurala en fazla **bir** bildirim oluşturulur; büyüklük sonradan düşse bile mevcut bildirim silinmez. |

> **BR-09 / BR-10 uç durumu:** AFAD bir depremi ilk olarak tahmini bir büyüklükle yayımlayıp sonradan düzeltebilir (ör. 3.9 → 4.1). "4.0 ve üzeri" kuralı olan kullanıcıya düzeltme sonrasında bildirim gitmelidir; ters yönde (4.1 → 3.9) daha önce oluşmuş bildirim geri alınmaz.

## 6. Kullanıcı Hikâyeleri

### US-04 — Otomatik güncelleme (FR-06) ✅

> **Bir** son kullanıcı **olarak**, yeni depremlerin kimse bir butona basmadan **en geç 1 dakika içinde** sisteme düşmesini **istiyorum**, **böylece** güncel deprem bilgisine hemen ulaşabileyim.

**Kabul kriterleri:**
- [x] Uygulama çalıştığı sürece sistem AFAD'ı **1 dakikada bir** kontrol eder. Bu süre ayar dosyasından (`Sync:IntervalMinutes`) değiştirilebilir.
- [x] Veritabanı boşsa, ilk açılışta **son 7 günün** depremleri otomatik olarak doldurulur.
- [x] Her kontrolde en son kayıtlı depremden **6 saat** geriye gidilerek tarama yapılır; böylece AFAD'ın sonradan düzelttiği büyüklükler de yakalanır (BR-04).
- [x] AFAD'a ulaşılamazsa hata log'a yazılır, uygulama kapanmaz ve bir sonraki turda tekrar denenir (NFR-03).
- [x] Her turun sonucu (kaç deprem geldiği, eklendiği, güncellendiği) log'a yazılır.
- [x] Otomatik güncelleme kopya kayıt oluşturmaz (BR-01).

## 7. Faz Durumu

| Faz | İçerik | Durum |
|---|---|---|
| 1 | AFAD'dan veri çekme, veritabanına kaydetme, kopya önleme (FR-01 – FR-05) | ✅ |
| 2 | Otomatik güncelleme (FR-06) | ✅ |
| 3 | Filtreli listeleme ve özet bilgiler (FR-07 – FR-09) | ⏳ |
| 4 | Kullanıcı kaydı / giriş (FR-10) | ⏳ |
| 5 | Alarm kuralları ve bildirimler (FR-11, FR-12) | ⏳ |
