#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using WebSecurity.Data;
using WebSecurity.Models;
using WebSecurity.Utilities;

namespace WebSecurity.Controllers
{
    public class ApplicationFilesController : Controller
    {
        private readonly WebSecurityContext _context;
        private readonly long fileSizeLimit = 10 * 1048576; 
        private readonly string[] permittedExtensions = { ".jpg" };

        public ApplicationFilesController(WebSecurityContext context)
        {
            _context = context;
        }


        // GET: ApplicationFiles
        public async Task<IActionResult> Index()
        {
            return View(await _context.ApplicationFiles.ToListAsync());
        }



        [HttpPost]
        [Route(nameof(UploadFile))]
        public async Task<IActionResult> UploadFile()
        {
            var theWebRequest = HttpContext.Request;

            // validation of Content-Type
            // 1. first, it must be a form-data request
            // 2. a boundary should be found in the Content-Type
            if (!theWebRequest.HasFormContentType ||
                !MediaTypeHeaderValue.TryParse(theWebRequest.ContentType, out var theMediaTypeHeader) ||
                string.IsNullOrEmpty(theMediaTypeHeader.Boundary.Value))
            {
                return new UnsupportedMediaTypeResult();
            }

            var reader = new MultipartReader(theMediaTypeHeader.Boundary.Value, theWebRequest.Body);
            var section = await reader.ReadNextSectionAsync();

            // This sample try to get the first file from request and save it
            // Make changes according to your needs in actual use
            while (section != null)
            {
                var DoesItHaveContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition,
                    out var theContentDisposition);

                if (DoesItHaveContentDispositionHeader && theContentDisposition.DispositionType.Equals("form-data") &&
                    !string.IsNullOrEmpty(theContentDisposition.FileName.Value))
                {
                    // Don't trust any file name, file extension, and file data from the request unless you trust them completely
                    // Otherwise, it is very likely to cause problems such as virus uploading, disk filling, etc
                    // In short, it is necessary to restrict and verify the upload
                    // Here, we just use the temporary folder and a random file name

                    ApplicationFile applicationFile = new ApplicationFile();
                    applicationFile.UntrustedName = HttpUtility.HtmlEncode(theContentDisposition.FileName.Value);
                    applicationFile.TimeStamp = DateTime.UtcNow;

                    applicationFile.Content =
                            await FileHelpers.ProcessStreamedFile(section, theContentDisposition,
                                ModelState, permittedExtensions, fileSizeLimit);
                    if (applicationFile.Content.Length == 0)
                    {
                        return RedirectToAction("Index", "ApplicationFiles");
                    }
                    applicationFile.Size = applicationFile.Content.Length;

                    await _context.ApplicationFiles.AddAsync(applicationFile);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index", "ApplicationFiles");

                }

                section = await reader.ReadNextSectionAsync();
            }

            // If the code runs to this location, it means that no files have been saved
            return BadRequest("No files data in the request.");
        }


        // GET: ApplicationFiles/Download/5
        public async Task<IActionResult> Download(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationFile = await _context.ApplicationFiles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (applicationFile == null)
            {
                return NotFound();
            }

            return File(applicationFile.Content, MediaTypeNames.Application.Octet, applicationFile.UntrustedName);
        }


        // GET: ApplicationFiles/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationFile = await _context.ApplicationFiles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (applicationFile == null)
            {
                return NotFound();
            }

            return View(applicationFile);
        }

        // GET: ApplicationFiles/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ApplicationFiles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UntrustedName,TimeStamp,Size,Content")] ApplicationFile applicationFile)
        {
            if (ModelState.IsValid)
            {
                applicationFile.Id = Guid.NewGuid();
                _context.Add(applicationFile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(applicationFile);
        }

        // GET: ApplicationFiles/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationFile = await _context.ApplicationFiles.FindAsync(id);
            if (applicationFile == null)
            {
                return NotFound();
            }
            return View(applicationFile);
        }

        // POST: ApplicationFiles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,UntrustedName,TimeStamp,Size,Content")] ApplicationFile applicationFile)
        {
            if (id != applicationFile.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(applicationFile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplicationFileExists(applicationFile.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(applicationFile);
        }

        // GET: ApplicationFiles/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationFile = await _context.ApplicationFiles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (applicationFile == null)
            {
                return NotFound();
            }

            return View(applicationFile);
        }

        // POST: ApplicationFiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var applicationFile = await _context.ApplicationFiles.FindAsync(id);
            _context.ApplicationFiles.Remove(applicationFile);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ApplicationFileExists(Guid id)
        {
            return _context.ApplicationFiles.Any(e => e.Id == id);
        }
    }
}
