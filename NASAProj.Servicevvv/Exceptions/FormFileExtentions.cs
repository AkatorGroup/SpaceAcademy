using Microsoft.AspNetCore.Http;
using NASAProj.Service.DTOs;

namespace ZaminEducation.Service.Extensions;

public static class FormFileExtentions
{
    public static AttachmentForCreationDTO ToAttachmentOrDefault(this IFormFile formFile)
    {

        if (formFile?.Length > 0)
        {
            using var ms = new MemoryStream();
            formFile.CopyTo(ms);
            var fileBytes = ms.ToArray();

            return new AttachmentForCreationDTO()
            {
                FileName = formFile.FileName,
                Stream = new MemoryStream(fileBytes)
            };
        }

        return null;
    }
}