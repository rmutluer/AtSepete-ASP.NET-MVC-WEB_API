﻿using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtSepete.Business.CloudinaryImageUploader
{
    public static class ImageUploaderService //bir bulut sunucusunda fotoğraflarımızı depolayacaktır.Cloudinary de!
    {
        private static Account account = new Account(
                 "df70ifqzq",
                 "991312396234646",
                  "sgSBwEpi-68kJQQG5jHsx4bnrHA");

        private static Cloudinary _cloudinary = new Cloudinary(account);

        public static async Task<string> SaveImageAsync(IFormFile image)
        {
            if (image == null) return null;

            var filePath = Path.GetTempFileName();//gecici dosya yolu oluşturur ve yolunu verir.
            using (var stream = File.Create(filePath))
            {
                await image.CopyToAsync(stream);//gelen image'i gecici açtığı dosyanın stream'ine kopyalıyor.
            }

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(filePath),//dosya yolunu veriyor
                PublicId = image.FileName//dosya ismi
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);//resmi yükler
            return uploadResult.Url.ToString();//resmin sonucunda json içindeki url'i çeker.
        }

        //public static async Task<string> SaveImageAsync(byte[] imageBytes, string fileName)
        //{
        //    if (imageBytes == null || imageBytes.Length == 0) return null;

        //    using (var memoryStream = new MemoryStream(imageBytes))
        //    {
        //        var formFile = new FormFile(memoryStream, 0, imageBytes.Length, null, fileName); // FormFile'u oluşturuyoruz

        //        var uploadParams = new ImageUploadParams
        //        {
        //            File = new FileDescription(fileName, formFile.OpenReadStream()), // FormFile'un stream'ini kullanıyoruz
        //            PublicId = fileName // Dosya ismi
        //        };

        //        var uploadResult = await _cloudinary.UploadAsync(uploadParams); // Resmi yüklüyoruz
        //        return uploadResult.Url.ToString(); // Resmin URL'sini döndürüyoruz
        //    }
        //}

    }
}
