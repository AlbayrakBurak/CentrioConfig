# Dynamic Configuration System

.NET 8 ile geliştirilmiş dinamik konfigürasyon kütüphanesi. Uygulama restart gerektirmeden konfigürasyon değerlerini güncelleyebilir ve yönetebilir.

## Özellikler

- ✅ **Dinamik Güncelleme**: Restart gerektirmeden konfigürasyon değişiklikleri
- ✅ **Tip Güvenliği**: Generic `GetValue<T>` metodu ile type-safe erişim
- ✅ **Periyodik Yenileme**: Parametrik olarak tanımlanan süre periyodunda otomatik kontrol
- ✅ **Last-Good Cache**: Storage erişemezse son başarılı kayıtlar ile çalışmaya devam eder
- ✅ **Thread-Safe**: `ImmutableDictionary` ve atomic swap ile concurrency-safe
- ✅ **Multi-Tenancy**: Her servis yalnızca kendi konfigürasyonlarına erişebilir
- ✅ **Admin UI**: Web arayüzü ile konfigürasyon yönetimi
- ✅ **REST API**: Swagger dokümantasyonlu API

## Proje Yapısı

```
src/
  Configuration.Core/          # Domain modelleri, interfaces
  Configuration.Infrastructure/ # MongoDB repository implementasyonu
  Configuration.Library/       # Ana DLL (ConfigurationReader)
  Admin.Api/                   # REST API (CRUD endpoints)
  Admin.Web/                   # React Admin UI

tests/
  Configuration.Library.Tests/ # Unit testler
```

## Gereksinimler

- .NET 8 SDK
- MongoDB (localhost:27017 veya connection string)
- Node.js 18+ (Admin.Web için)

## Kurulum

### 1. MongoDB Kurulumu

MongoDB'nin çalışıyor olduğundan emin olun:

```bash
# MongoDB bağlantısını test et
mongosh mongodb://localhost:27017
```

### 2. Projeyi Çalıştırma

#### Admin API

```bash
cd src/Admin.Api
dotnet run
```

API: `http://localhost:5079`  
Swagger: `http://localhost:5079/swagger`

#### Admin Web

```bash
cd src/Admin.Web
npm install
npm run dev
```

Web UI: `http://localhost:5173`

## Kullanım

### ConfigurationReader Kullanımı

```csharp
using Configuration.Library;

// 1. ConfigurationReader oluştur
var reader = new ConfigurationReader(
    applicationName: "SERVICE-A",
    connectionString: "mongodb://localhost:27017",
    refreshTimerIntervalInMs: 30000  // 30 saniye
);

// 2. Değerleri oku
string siteName = reader.GetValue<string>("SiteName");
int maxCount = reader.GetValue<int>("MaxItemCount");
bool isEnabled = reader.GetValue<bool>("IsBasketEnabled");

// 3. Dispose (uygulama kapanırken)
await reader.DisposeAsync();
```

### Desteklenen Tipler

- `string`
- `int` / `int?`
- `double` / `double?`
- `bool` / `bool?`

### TryGetValue (Non-throwing)

```csharp
if (reader.TryGetValue<string>("OptionalKey", out var value))
{
    Console.WriteLine($"Value: {value}");
}
else
{
    Console.WriteLine("Key not found");
}
```

## Admin API Endpoints

### GET `/{applicationName}/configs`
Liste tüm aktif konfigürasyonları.

**Query Parameters:**
- `name` (optional): İsme göre filtreleme
- `includeInactive` (optional): Pasif kayıtları da getir

**Örnek:**
```bash
GET /SERVICE-A/configs?name=Site
```

### POST `/{applicationName}/configs`
Yeni konfigürasyon kaydı oluştur.

**Request Body:**
```json
{
  "name": "SiteName",
  "type": 0,
  "value": "soty.io",
  "isActive": true
}
```

**Type Değerleri:**
- `0`: String
- `1`: Int
- `2`: Double
- `3`: Bool

### PUT `/{applicationName}/configs/{id}`
Konfigürasyon kaydını güncelle.

### PATCH `/{applicationName}/configs/{id}/activate?isActive={true|false}`
Konfigürasyon kaydını aktif/pasif yap.

## Testler

```bash
# Tüm testleri çalıştır
dotnet test

# Belirli bir test projesini çalıştır
dotnet test tests/Configuration.Library.Tests
```

## Tasarım Desenleri

- **Repository Pattern**: `IConfigurationRepository` ile data access abstraction
- **Strategy Pattern**: `ITypeConverter` ile tip dönüşümü
- **Decorator Pattern**: `ResilientMongoConfigurationRepository` wraps `MongoConfigurationRepository`
- **Clean Architecture**: Core, Infrastructure, Library katmanları

## Güvenlik

- Her servis yalnızca kendi `applicationName`'ine ait kayıtlara erişebilir
- Repository seviyesinde ve API seviyesinde `applicationName` kontrolü yapılır

## Performans

- In-memory cache (`ImmutableDictionary`) ile O(1) okuma performansı
- Atomic swap ile lock-free okuma
- Delta refresh ile sadece değişen kayıtlar çekilir

## Teknik Detaylar

### Thread Safety

```csharp
// Lock-free okuma
var snapshot = _cache;  // ImmutableDictionary snapshot
var value = snapshot[key];

// Atomic swap
Interlocked.Exchange(ref _cache, newCache);
```

### Last-Good Cache

Storage erişemezse, son başarılı yüklenen cache korunur ve servis edilmeye devam eder.

### Periyodik Kontrol

```csharp
var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(refreshIntervalMs));
while (await timer.WaitForNextTickAsync(cancellationToken))
{
    await RefreshDeltaAsync(cancellationToken);  // Sadece değişenler
}
```

## Lisans


