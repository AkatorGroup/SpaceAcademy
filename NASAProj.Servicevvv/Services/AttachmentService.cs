using NASAProj.Data.IRepositories;
using NASAProj.Domain.Models;
using NASAProj.Service.DTOs;
using NASAProj.Service.Exceptions;
using System.Linq.Expressions;
using NASAProj.Service.Helpers;
using NASAProj.Service.Interfaces;

namespace NASAProj.Service.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IGenericRepository<Attachment> _repository;

    public AttachmentService(IGenericRepository<Attachment> repository)
    {
        _repository = repository;
    }

    public async ValueTask<Attachment> CreateAsync(string fileName, string filePath)
    {
        var file = new Attachment()
        {
            Name = fileName,
            Path = filePath
        };

        file = await _repository.AddAsync(file);
        await _repository.SaveChangesAsync();

        return file;
    }

    public async ValueTask<bool> DeleteAsync(Expression<Func<Attachment, bool>> expression)
    {
        var file = await _repository.GetAsync(expression);

        if (file is null)
            throw new HttpStatusCodeException(404, "Attachment not found");

        // delete file from wwwroot
        string fullPath = Path.Combine(EnvironmentHelper.WebRootPath, file.Path);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        // datele database information
        FileHelper.Remove(file.Path);

        _repository.Delete(file);
        await _repository.SaveChangesAsync();

        return true;
    }

    public async ValueTask<Attachment> UploadAsync(AttachmentForCreationDTO dto)
    {
        // genarate file destination
        string fileName = Guid.NewGuid().ToString("N") + "-" + dto.FileName;
        string filePath = Path.Combine(EnvironmentHelper.AttachmentPath, fileName);

        if (!Directory.Exists(EnvironmentHelper.AttachmentPath))
            Directory.CreateDirectory(EnvironmentHelper.AttachmentPath);

        // copy image to the destination as stream
        FileStream fileStream = File.OpenWrite(filePath);
        await dto.Stream.CopyToAsync(fileStream);

        // clear
        await fileStream.FlushAsync();
        fileStream.Close();

        return await CreateAsync(fileName, Path.Combine(EnvironmentHelper.FilePath, fileName));
    }

    public async ValueTask<Attachment> GetAsync(Expression<Func<Attachment, bool>> expression)
    {
        var existAttachement = await _repository.GetAsync(expression);

        if (existAttachement is null)
            throw new HttpStatusCodeException(404, "Attachment not found.");

        return existAttachement;
    }

    public async ValueTask<Attachment> UpdateAsync(int id, Stream stream)
    {
        var existAttachment = await _repository.GetAsync(a => a.Id == id, null);

        if (existAttachment is null)
            throw new HttpStatusCodeException(404, "Attachment not found.");

        string fileName = existAttachment.Path;
        string filePath = Path.Combine(EnvironmentHelper.WebRootPath, fileName);

        // copy image to the destination as stream
        FileStream fileStream = File.OpenWrite(filePath);
        await stream.CopyToAsync(fileStream);

        // clear
        await fileStream.FlushAsync();
        fileStream.Close();

        existAttachment.Update();
        await _repository.SaveChangesAsync();

        return existAttachment;
    }
}
