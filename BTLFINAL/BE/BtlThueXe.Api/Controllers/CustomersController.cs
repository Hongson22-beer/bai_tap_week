using System.Security.Claims;
using BtlThueXe.Core.DTOs.Customers;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController][Route("api/[controller]")][Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    public CustomersController(ICustomerService customerService)=>_customerService=customerService;
    private int UserId(){var s=User.FindFirstValue(ClaimTypes.NameIdentifier)??User.FindFirstValue("sub"); return int.TryParse(s,out var id)?id:throw new UnauthorizedAccessException();}

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(){var p=await _customerService.GetProfileByUserIdAsync(UserId());return p==null?NotFound(new{message="Hồ sơ khách hàng chưa được khởi tạo."}):Ok(p);}
    [HttpPut("me")][Authorize(Roles="KHACH_HANG")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateCustomerProfileRequestDto dto){try{return Ok(await _customerService.UpdateProfileAsync(UserId(),dto));}catch(ArgumentException ex){return BadRequest(new{message=ex.Message});}catch(KeyNotFoundException ex){return NotFound(new{message=ex.Message});}}

    [HttpPost("me/documents/{documentType}")][Authorize(Roles="KHACH_HANG")][RequestSizeLimit(5_500_000)]
    public async Task<IActionResult> UploadDocument(string documentType,IFormFile file)
    {
        if(file==null||file.Length==0)return BadRequest(new{message="Vui lòng chọn ảnh."}); if(file.Length>5_000_000)return BadRequest(new{message="Ảnh tối đa 5 MB."});
        var ext=Path.GetExtension(file.FileName).ToLowerInvariant(); if(ext is not ".jpg" and not ".jpeg" and not ".png")return BadRequest(new{message="Chỉ chấp nhận JPG, JPEG hoặc PNG."});
        await using var s=file.OpenReadStream(); try{var token=await _customerService.SaveDocumentAsync(UserId(),documentType,s,ext);return Ok(new{message="Tải ảnh giấy tờ thành công.",documentType,token});}catch(ArgumentException ex){return BadRequest(new{message=ex.Message});}
    }
    [HttpGet("me/documents/{documentType}")][Authorize(Roles="KHACH_HANG")]
    public async Task<IActionResult> MyDocument(string documentType){var f=await _customerService.GetDocumentAsync(UserId(),documentType,false);return f==null?NotFound():File(f.Value.Bytes,f.Value.ContentType);}

    [HttpGet][Authorize(Roles="ADMIN,NHAN_VIEN")]
    public async Task<IActionResult> GetAll()=>Ok(await _customerService.GetAllCustomersAsync());
    [HttpGet("{id:int}/documents/{documentType}")][Authorize(Roles="ADMIN,NHAN_VIEN")]
    public async Task<IActionResult> StaffDocument(int id,string documentType){var f=await _customerService.GetDocumentAsync(id,documentType,true);return f==null?NotFound():File(f.Value.Bytes,f.Value.ContentType);}
    [HttpPatch("{id}/verify-cccd")][Authorize(Roles="ADMIN,NHAN_VIEN")]
    public async Task<IActionResult> VerifyCccd(int id,[FromBody] VerifyCccdRequestDto dto)=>await _customerService.VerifyCccdAsync(id,dto.DaXacMinh)?Ok(new{message="Cập nhật xác minh CCCD thành công."}):NotFound();
    [HttpPatch("{id}/verify-documents")][Authorize(Roles="ADMIN,NHAN_VIEN")]
    public async Task<IActionResult> VerifyDocuments(int id,[FromBody] VerifyDocumentsRequestDto dto)=>await _customerService.VerifyDocumentsAsync(id,dto.CccdDaXacMinh,dto.GplxDaXacMinh)?Ok(new{message="Cập nhật xác minh hồ sơ thuê xe thành công."}):NotFound();
}
