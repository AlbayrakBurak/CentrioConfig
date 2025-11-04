using Configuration.Library;

Console.WriteLine("=== ConfigurationReader Test Uygulaması ===\n");

const string applicationName = "SERVICE-A";
const string connectionString = "mongodb://localhost:27017";
const int refreshIntervalMs = 30000; // 30 saniye

Console.WriteLine($"Application Name: {applicationName}");
Console.WriteLine($"Connection String: {connectionString}");
Console.WriteLine($"Refresh Interval: {refreshIntervalMs}ms\n");

Console.WriteLine("ConfigurationReader oluşturuluyor...");
var reader = new ConfigurationReader(applicationName, connectionString, refreshIntervalMs);

Console.WriteLine("İlk yükleme için 3 saniye bekleniyor...\n");
await Task.Delay(3000);

try
{
    Console.WriteLine("=== GetValue<T> Testleri ===\n");

    // PDF'teki örnek kullanım: _configurationReader.GetValue("SiteName");
    // Not: Generic tip belirtilmeli: GetValue<string>("SiteName")
    try
    {
        var siteName = reader.GetValue<string>("SiteName");
        Console.WriteLine($"✅ SiteName (GetValue<string>): {siteName}");
        Console.WriteLine($"   PDF örneği: _configurationReader.GetValue<string>(\"SiteName\") → \"{siteName}\"");
    }
    catch (KeyNotFoundException)
    {
        Console.WriteLine("❌ SiteName bulunamadı!");
        Console.WriteLine("   Swagger'dan POST /SERVICE-A/configs ile SiteName ekleyin:");
        Console.WriteLine("   { \"name\": \"SiteName\", \"type\": 0, \"value\": \"soty.io\", \"isActive\": true }");
    }

    // Int testi
    if (reader.TryGetValue<int>("MaxItemCount", out var maxCount))
    {
        Console.WriteLine($"✅ MaxItemCount (int): {maxCount}");
    }
    else
    {
        Console.WriteLine("⚠️  MaxItemCount bulunamadı (opsiyonel)");
    }

    // Double testi
    if (reader.TryGetValue<double>("Price", out var price))
    {
        Console.WriteLine($"✅ Price (double): {price}");
    }
    else
    {
        Console.WriteLine("⚠️  Price bulunamadı (opsiyonel)");
    }

    // Bool testi
    if (reader.TryGetValue<bool>("IsBasketEnabled", out var isEnabled))
    {
        Console.WriteLine($"✅ IsBasketEnabled (bool): {isEnabled}");
    }
    else
    {
        Console.WriteLine("⚠️  IsBasketEnabled bulunamadı (opsiyonel)");
    }

    Console.WriteLine("\n=== Dinamik Güncelleme Testi ===");
    Console.WriteLine("Swagger'dan SiteName değerini değiştirin, 30 saniye sonra burada tekrar okuyacağız...\n");

    Console.WriteLine("İlk değer:");
    if (reader.TryGetValue<string>("SiteName", out var initialValue))
    {
        Console.WriteLine($"  SiteName: {initialValue}");
    }

    Console.WriteLine("\n30 saniye bekleniyor (refresh interval)...");
    Console.WriteLine("Bu süre içinde Swagger'dan SiteName'i güncelleyin!");
    Console.WriteLine("(http://localhost:5079/swagger)\n");

    for (int i = 30; i > 0; i--)
    {
        Console.Write($"\rKalan: {i} saniye... ");
        await Task.Delay(1000);
    }
    Console.WriteLine("\n");

    Console.WriteLine("Güncellenmiş değer:");
    if (reader.TryGetValue<string>("SiteName", out var updatedValue))
    {
        Console.WriteLine($"  SiteName: {updatedValue}");
        if (updatedValue != initialValue)
        {
            Console.WriteLine("✅ Dinamik güncelleme başarılı! (Restart gerektirmedi)");
        }
        else
        {
            Console.WriteLine("⚠️  Değer değişmemiş (Swagger'dan güncelleme yapıldı mı?)");
        }
    }

    Console.WriteLine("\n=== GetValue<T> (Exception Test) ===");
    try
    {
        var nonExistent = reader.GetValue<string>("NonExistentKey");
        Console.WriteLine($"❌ Hata: KeyNotFoundException fırlatılmalıydı!");
    }
    catch (KeyNotFoundException ex)
    {
        Console.WriteLine($"✅ KeyNotFoundException yakalandı: {ex.Message}");
    }

    Console.WriteLine("\n✅ Tüm testler tamamlandı!");
    Console.WriteLine("\nÇıkmak için herhangi bir tuşa basın...");
    Console.ReadKey();
}
finally
{
    Console.WriteLine("\nConfigurationReader dispose ediliyor...");
    await reader.DisposeAsync();
    Console.WriteLine("✅ Temizlik tamamlandı.");
}

