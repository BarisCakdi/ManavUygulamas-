using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Ana Sayfa";
        return View(UrunleriGetir());
    }

    [HttpPost]
    public IActionResult SatinAl(SatisModel model)
    {
        ViewData["Title"] = "Satın Al";
        var urunler = UrunleriGetir();

        if (!ModelState.IsValid)
        {
            ViewData["Hata"] = "Ürün seçimi yapılmadı.";
            return View("Index", urunler);
        }
        Urun? alinacakUrun = null;
        foreach (var urun in urunler)
        {
            if (urun.Ad == model.Ad)
            {
                alinacakUrun = urun;
            }
        }

        if (alinacakUrun.Fiyat > model.Para)
        {
            ViewData["Hata"] = "Yetersiz bakiye! 😢";
            return View();
        }
        if (alinacakUrun == null)
        {
            ViewData["Hata"] = "Böyle bir ürün bulunamadı!";
            // kontrole bağlı olarak akışı kesmemiz gerekiyorsa return demeliyiz. yoksa aşağıdaki kodlar çalışmaya devam eder. ürünü bulamadıysak stok kontrolü yapmamalıyız!
            return View();
        }

        if (alinacakUrun.Stok < 1)
        {
            ViewData["Hata"] = "Bu ürün kalmadı 😢";
            return View();
        }

        alinacakUrun.Stok--;
        DegisiklikleriKaydet(urunler);
        SatisEkle(alinacakUrun);

        return View();
    }

    public IActionResult Rapor()
    {
        ViewData["Title"] = "Rapor";
        var satislar = new List<Satis>();
        var toplamSatis = 0;
        using StreamReader reader = new("App_Data/satislar.txt");
        var txt = reader.ReadToEnd();

        var lines = txt.Split('\n');
        foreach (var line in lines)
        {
            var urunAdi = line.Split('|')[0];
            var fiyat = int.Parse(line.Split('|')[1]);
            toplamSatis += fiyat;
            //henüz benzer üründe satıi kaydı yoksa
            Satis? oncekiSatis = SatisKaydiBul(urunAdi, satislar);
            if (oncekiSatis == null)
            {
                satislar.Add(
               new Satis
               {
                   UrunAdi = urunAdi,
                   ToplamSatis = fiyat,
                   Adet = 1
               }
               );
            }
            else
            {
                oncekiSatis.ToplamSatis += fiyat;
                oncekiSatis.Adet++;
                //ToplamSatis += fiyat;
                //Adet++
            }



        }
        ViewBag.Toplam = toplamSatis;
        return View(satislar);
    }

    [HttpGet]
    public IActionResult UrunEkle()
    {
        ViewData["Title"] = "Urun Ekle";
        return View(UrunleriGetir());
    }
    [HttpPost]
    public IActionResult UrunEkle(Urun model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Hata"] = "Ürün bilgileri eksik veya hatalı.";
            return View();
        }

        // Ürün varlığını kontrol et
        var urunler = UrunleriGetir();
        if (urunler.Any(u => u.Ad == model.Ad))
        {
            ViewData["Hata"] = "Bu ürün adı zaten mevcut.";
            return View();
        }

        // Yeni ürünü oluştur ve listeye ekle
        var urun = new Urun
        {
            Ad = model.Ad,
            Fiyat = model.Fiyat,
            Stok = model.Stok
        };
        urunler.Add(urun);

        // Değişiklikleri kaydet
        DegisiklikleriKaydet(urunler);

        ViewData["Basari"] = "Ürün başarıyla eklendi.";
        return View("UrunGuncelle", urunler);
    }
    //public IActionResult UrunSil()
    //{
    //    ViewData["Title"] = "Urun Sil";
    //    return View(UrunleriGetir());
    //}
    [HttpPost]
    public IActionResult UrunSil(Urun model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Hata"] = "Ürün bilgileri eksik veya hatalı.";
            return View();
        }
        // Ürün varlığını kontrol et
        var urunler = UrunleriGetir();
        for (int i = 0; i < urunler.Count; i++)
        {
            if (urunler[i].Ad == model.Ad)
            {
                urunler.RemoveAt(i);
                ViewData["Mesaj"] = "Ürün Silindi!";
                break;
            }
        }
        DegisiklikleriKaydet(urunler);
        return View("UrunGuncelle", urunler);
    }
    public IActionResult UrunGuncelle()
    {
        ViewData["Title"] = "Admin";
        return View(UrunleriGetir());
    }

    [HttpPost]
    public IActionResult UrunGuncelle(Urun model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Hata"] = "Ürün bilgileri eksik veya hatalı.";
            return View();
        }

        // Güncellenecek ürünü bul
        var urunler = UrunleriGetir();
        var guncellenecekUrun = urunler.FirstOrDefault(u => u.Ad == model.Ad);
        if (guncellenecekUrun == null)
        {
            ViewData["Hata"] = "Güncellenecek ürün bulunamadı.";
            return View();
        }

        // Ürün bilgilerini güncelle
        guncellenecekUrun.Fiyat = model.Fiyat;
        guncellenecekUrun.Stok = model.Stok;

        // Değişiklikleri kaydet
        DegisiklikleriKaydet(urunler);

        ViewData["Basari"] = "Ürün başarıyla güncellendi.";
        return View("UrunGuncelle", urunler);
    }

    public Satis? SatisKaydiBul(string urunAdi, List<Satis> satislar)
    {
        foreach (var satis in satislar)
        {
            if (satis.UrunAdi == urunAdi)
            {
                return satis;
            }
        }

        return null;
    }

    public void SatisEkle(Urun urun)
    {
        using StreamReader reader = new("App_Data/satislar.txt");
        var satislarTxt = reader.ReadToEnd();
        reader.Close();

        //eğer metin dosyamızın içinde daha önce kayıt varsa
        //yeni eklediğimiz satırların yeni satır olarak eklenmesini sağlamamız lazım
        //eğer olduğu gibi eklersek yeni satıra eklemiyor
        //bu yüzden ekleme yapmadan yeni bir enter - satır eklemesi yapıyoruz
        if (!string.IsNullOrEmpty(satislarTxt))
        {
            satislarTxt += "\n";
        }

        using StreamWriter writer = new("App_Data/satislar.txt");
        writer.Write($"{satislarTxt}{urun.Ad}|{urun.Fiyat}");
    }

    public void DegisiklikleriKaydet(List<Urun> urunler)
    {
        var satirlarTxt = "";
        foreach (var urun in urunler)
        {
            satirlarTxt += $"{urun.Ad}|{urun.Fiyat}|{urun.Stok}{(urun != urunler.Last() ? "\n" : "")}";
        }

        // // ürünlerin sayısı kadar bir string dizisi oluşturuyorum.
        // var satirlar = new string[urunler.Count];
        //
        // for (var i = 0; i < urunler.Count; i++)
        // {
        //     var urun = urunler[i];
        //     // Elma|10|5
        //     satirlar[i] = $"{urun.Ad}|{urun.Fiyat}|{urun.Stok}";
        //     // oluşturduğum her bir satırı for üstündeki kısımda tanımladığım string dizisinin içine yerleştiriyorum
        // }
        //
        // // string dizisini tek bir parça string - metin haline getiriyorum.
        // var satirlarTxt = string.Join('\n', satirlar);
        // // bunu yapma sebebim en sona \n eklememek

        // metnimi txt içine yazdırıyorum.
        using StreamWriter writer = new("App_Data/urunler.txt");
        writer.Write(satirlarTxt);
    }

    public List<Urun> UrunleriGetir()
    {
        var urunler = new List<Urun>();

        using StreamReader reader = new("App_Data/urunler.txt");
        var urunlerTxt = reader.ReadToEnd();
        var urunlerSatirlar = urunlerTxt.Split('\n');
        foreach (var satir in urunlerSatirlar)
        {
            var urunSatir = satir.Split('|');
            urunler.Add(new Urun
            {
                Ad = urunSatir[0],
                Fiyat = int.Parse(urunSatir[1]),
                Stok = int.Parse(urunSatir[2])
            });
        }

        return urunler;
    }
}