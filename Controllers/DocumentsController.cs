using EMS.Api.Data;
using EMS.Api.DTOs.Documents;
using EMS.Api.Hubs; // 1. Hub ko import karein
using EMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; // 2. SignalR ko import karein
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IHubContext<NotificationHub> _hubContext; // 3. HubContext variable banayein
    private readonly ILogger<DocumentsController> _logger; // Logger add karein

    // 4. Constructor mein sabko inject karein
    public DocumentsController(ApplicationDbContext context, IWebHostEnvironment env, IHubContext<NotificationHub> hubContext, ILogger<DocumentsController> logger)
    {
        _context = context;
        _env = env;
        _hubContext = hubContext;
        _logger = logger;
    }

    // GET: api/Documents - Gets documents visible to the logged-in user
    [HttpGet]
    public async Task<IActionResult> GetDocuments()
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        var query = _context.Documents.Where(d => d.CompanyId == companyId);

        if (userRole == "employee" || userRole == "department_manager")
        {
            query = query.Where(d => d.EmployeeId == userId || d.EmployeeId == null);
        }

        var documents = await query
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return Ok(documents);
    }

    // POST: api/Documents/upload - Uploads a new document
    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadDto docDto)
    {
        try
        {
            var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
            var uploaderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var uploaderRole = User.FindFirstValue(ClaimTypes.Role)!;

            int? targetEmployeeId = docDto.EmployeeId;

            if (uploaderRole == "employee" || uploaderRole == "department_manager")
            {
                targetEmployeeId = uploaderId;
            }

            if (docDto.File == null || docDto.File.Length == 0)
                return BadRequest("No file uploaded.");

            var uniqueFileName = $"{Guid.NewGuid()}_{docDto.File.FileName}";
            var companyUploadsPath = Path.Combine(_env.WebRootPath, "documents", companyId.ToString());
            Directory.CreateDirectory(companyUploadsPath);
            var filePath = Path.Combine(companyUploadsPath, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await docDto.File.CopyToAsync(stream);
            }

            var document = new Document
            {
                CompanyId = companyId,
                EmployeeId = targetEmployeeId,
                DocumentType = docDto.DocumentType,
                DocumentName = docDto.File.FileName,
                FileUrl = $"/documents/{companyId}/{uniqueFileName}",
                UploadedAt = DateTime.UtcNow,
                UploadedById = uploaderId
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // 5. Signal bhejein
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "DocumentUpdated");

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while uploading document {FileName}", docDto.File.FileName);
            return StatusCode(500, "An internal server error occurred during file upload.");
        }
    }

    // DELETE: api/Documents/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        try
        {
            var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);

            var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);
            if (document == null) return NotFound("Document not found in your company.");

            if (!string.IsNullOrEmpty(document.FileUrl))
            {
                var physicalPath = Path.Combine(_env.WebRootPath, document.FileUrl.TrimStart('/'));
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            // 6. Signal bhejein
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "DocumentUpdated");

            return Ok(new { message = "Document deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting document {DocumentId}", id);
            return StatusCode(500, "An internal server error occurred.");
        }
    }
}