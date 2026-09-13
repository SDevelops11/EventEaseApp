namespace EventEaseApp.Services
{
    public interface IBlobService
    {
        Task<string> UploadFileAsync(IFormFile file, string folderName);
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}
