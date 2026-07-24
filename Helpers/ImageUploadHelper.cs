namespace Source.Helpers
{
    public static class ImageUploadHelper
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg"
        };

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif", "image/svg+xml"
        };

        public const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public static (bool IsValid, string? ErrorMessage) Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return (true, null);
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return (false, "Kích thước ảnh không được vượt quá 5MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                return (false, "Chỉ chấp nhận file ảnh: JPG, PNG, WEBP, GIF, SVG.");
            }

            if (!string.IsNullOrEmpty(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
            {
                return (false, "Định dạng file ảnh không hợp lệ.");
            }

            return (true, null);
        }

        public static async Task<string?> SaveToWwwRootAsync(IFormFile file, string webRootPath, string subFolder = "images")
        {
            var validation = Validate(file);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.ErrorMessage);
            }

            var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
            var directory = Path.Combine(webRootPath, subFolder);
            Directory.CreateDirectory(directory);

            var filePath = Path.Combine(directory, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/{subFolder}/{fileName}";
        }
    }
}
