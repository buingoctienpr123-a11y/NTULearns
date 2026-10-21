namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Services
{
    // Lưu trữ file tài liệu bài học ngoài wwwroot
    // để người dùng không thể tải trực tiếp qua URL tĩnh.
    public class LessonFileStorage
    {
        public const long MaxFileSize = 20 * 1024 * 1024;

        public static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".doc", ".docx", ".ppt", ".pptx",
                ".xls", ".xlsx", ".zip", ".txt"
            };

        private readonly string _rootPath;

        public LessonFileStorage(
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            var configured = configuration["FileStorage:LessonFilesPath"];

            _rootPath = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(env.ContentRootPath, "App_Data", "LessonFiles")
                : Path.GetFullPath(configured, env.ContentRootPath);

            Directory.CreateDirectory(_rootPath);
        }

        public static bool IsAllowedExtension(string fileName)
        {
            return AllowedExtensions.Contains(
                Path.GetExtension(fileName));
        }

        // Lưu file với tên ngẫu nhiên, trả về tên đã lưu
        public async Task<string> SaveAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            var storedName = Guid.NewGuid().ToString("N") + extension;

            await using var stream = new FileStream(
                GetPath(storedName),
                FileMode.CreateNew);

            await file.CopyToAsync(stream);

            return storedName;
        }

        public string GetPath(string storedFileName)
        {
            // Chỉ dùng tên file, không cho phép đường dẫn con
            return Path.Combine(
                _rootPath,
                Path.GetFileName(storedFileName));
        }

        public void Delete(string storedFileName)
        {
            var path = GetPath(storedFileName);

            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
