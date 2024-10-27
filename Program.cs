

class Program
{
    static async Task Main(string[] args)
    {
        string url;
        int parts;

        // Daha önce kaydedilmiş URL'yi oku
        if (File.Exists("download_url.txt"))
        {
            url = File.ReadAllText("download_url.txt");
            Console.WriteLine($"İndirme devam ediyor: {url}");
        }
        else
        {
            Console.WriteLine("Dosya URL'sini girin:");
            url = Console.ReadLine();
            File.WriteAllText("download_url.txt", url); // URL'yi dosyaya kaydet
        }

        // Daha önce kaydedilmiş parça sayısını oku
        if (File.Exists("download_part_count.txt"))
        {
            parts = int.Parse(File.ReadAllText("download_part_count.txt"));
            Console.WriteLine($"İndirme {parts} parçayla devam ediyor.");
        }
        else
        {
            Console.WriteLine("İndirmeyi kaç parçaya ayırmak istersiniz?");
            if (!int.TryParse(Console.ReadLine(), out parts) || parts < 1)
            {
                Console.WriteLine("Geçersiz parça sayısı. Varsayılan olarak 1 parça kullanılıyor.");
                parts = 1;
            }
            File.WriteAllText("download_part_count.txt", parts.ToString()); // Parça sayısını dosyaya kaydet
        }

        // Dosyayı Kullanıcının Downloads klasörüne kaydet
        string downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
        string destinationPath = Path.Combine(downloadsFolder, "downloaded_file");

        var downloader = new FileDownloader();
        await downloader.DownloadFileAsync(url, destinationPath, parts);
    }
}