using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

public class FileDownloader
{
    private const string ProgressFileName = "download_progress.txt"; // İndirme ilerlemesini saklamak için dosya
    private const string UrlFileName = "download_url.txt"; // İndirme URL'sini saklamak için dosya
    private const string PartCountFileName = "download_part_count.txt"; // Parça sayısını saklamak için dosya

    public async Task DownloadFileAsync(string url, string destinationPath, int parts)
    {
        using (HttpClient httpClient = new HttpClient())
        {
            // Toplam dosya boyutunu almak için HEAD isteği gönder
            var headResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
            headResponse.EnsureSuccessStatusCode();
            long totalFileSize = headResponse.Content.Headers.ContentLength ?? -1;

            if (totalFileSize == -1)
            {
                Console.WriteLine("Dosya boyutu alınamadı.");
                return;
            }

            Console.WriteLine($"Toplam dosya boyutu: {totalFileSize} bayt");

            // Her parçanın boyutunu hesapla
            long partSize = totalFileSize / parts;
            var downloadTasks = new List<Task>();
            long totalReadBytes = 0;

            // İlerleme dosyası varsa oku
            if (File.Exists(ProgressFileName))
            {
                string progress = File.ReadAllText(ProgressFileName);
                if (long.TryParse(progress, out long savedBytes))
                {
                    totalReadBytes = savedBytes;
                    Console.WriteLine($"İndirme {totalReadBytes} bayttan devam ediyor.");
                }
            }

            // Hedef dosyayı oluştur veya üzerine yaz
            using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                fs.SetLength(totalFileSize); // Alanı önceden ayarla
            }

            // Her parçayı paralel olarak indir
            for (int i = 0; i < parts; i++)
            {
                long startByte = i * partSize;
                long endByte = (i == parts - 1) ? totalFileSize - 1 : startByte + partSize - 1;

                // Eğer parça zaten indirilmişse, atla
                if (totalReadBytes >= endByte + 1)
                {
                    Console.WriteLine($"Parça {i + 1} zaten indirildi.");
                    continue;
                }

                downloadTasks.Add(DownloadPartAsync(url, destinationPath, startByte, endByte, totalFileSize, totalReadBytes, i + 1));
            }

            // Tüm parçaların indirilmesini bekle
            await Task.WhenAll(downloadTasks);

            Console.WriteLine("İndirme tamamlandı!");
            File.Delete(ProgressFileName); // İndirme tamamlandığında ilerleme dosyasını sil
            File.Delete(UrlFileName); // URL dosyasını sil
            File.Delete(PartCountFileName); // Parça sayısını sil
        }
    }

    private async Task DownloadPartAsync(string url, string destinationPath, long startByte, long endByte, long totalFileSize, long totalReadBytes, int partNumber)
    {
        using (HttpClient httpClient = new HttpClient())
        {
            // Belirli byte aralığı için Range başlığını ayarla
            httpClient.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(startByte, endByte);

            // Parçayı indir
            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (var fs = new FileStream(destinationPath, FileMode.Open, FileAccess.Write, FileShare.Write))
            {
                // Dosya akışını bu parçanın başlangıç byte'ında konumlandır
                fs.Position = startByte;

                byte[] buffer = new byte[8192]; // 8 KB tampon
                int readBytes;
                long totalDownloaded = totalReadBytes;

                Console.WriteLine($"Parça {partNumber} indiriliyor...");

                while ((readBytes = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, readBytes); // İndirilen parçayı dosyaya yaz
                    totalDownloaded += readBytes; // Toplam indirilen baytları güncelle

                    // İlerlemeyi kaydet
                    File.WriteAllText(ProgressFileName, totalDownloaded.ToString());

                    // İlerleme yüzdesini hesapla ve göster
                    double progressPercentage = (double)totalDownloaded / totalFileSize * 100;
                    Console.WriteLine($"Parça {partNumber} - İndirildi: {totalDownloaded} bayt ({progressPercentage:F2}%)");
                }
            }
        }
    }
}

