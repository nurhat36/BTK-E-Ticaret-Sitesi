using BTKETicaretSitesi.Data;
using BTKETicaretSitesi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BTKETicaretSitesi.Controllers
{
    [Authorize]
    public class AddressController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AddressController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Kullanıcının adres listesi
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var addresses = await _context.Addresses
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .ToListAsync();

            return View(addresses);
        }

        // Yeni adres formu
        public IActionResult Create()
        {
            return View();
        }

        // Adres oluşturma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Address address)
        {
            if (!ModelState.IsValid)
            {
                address.UserId = _userManager.GetUserId(User);
                _context.Add(address);
                await _context.SaveChangesAsync();

                // Eğer varsayılan adres olarak işaretlendiyse
                if (address.IsDefault)
                {
                    await SetAsDefaultAddress(address.Id, address.AddressType);
                }

                return RedirectToAction(nameof(Index));
            }
            return View(address);
        }

        // Adres düzenleme formu
        public async Task<IActionResult> Edit(int? id)
        {
            var userId = _userManager.GetUserId(User);
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }
            return View(address);
        }

        // Adres güncelleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Address address)
        {
            if (id != address.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    address.UserId = _userManager.GetUserId(User);
                    _context.Update(address);
                    await _context.SaveChangesAsync();

                    if (address.IsDefault)
                    {
                        await SetAsDefaultAddress(address.Id, address.AddressType);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AddressExists(address.Id))
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
            return View(address);
        }

        // Adres silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address != null)
            {
                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Varsayılan adres olarak ayarla
        public async Task<IActionResult> SetAsDefault(int id)
        {
            var userId = _userManager.GetUserId(User);
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address != null)
            {
                await SetAsDefaultAddress(address.Id, address.AddressType);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task SetAsDefaultAddress(int addressId, AddressType addressType)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            // İlgili adres tipindeki diğer adreslerin varsayılanlığını kaldır
            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId &&
                           (a.AddressType == addressType || a.AddressType == AddressType.Both) &&
                           a.Id != addressId)
                .ToListAsync();

            foreach (var addr in addresses)
            {
                addr.IsDefault = false;
            }

            // User'da varsayılan adres bilgisini güncelle
            if (addressType == AddressType.Shipping || addressType == AddressType.Both)
            {
                user.DefaultShippingAddressId = addressId;
            }

            if (addressType == AddressType.Billing || addressType == AddressType.Both)
            {
                user.DefaultBillingAddressId = addressId;
            }

            await _context.SaveChangesAsync();
            await _userManager.UpdateAsync(user);
        }

        private bool AddressExists(int id)
        {
            return _context.Addresses.Any(e => e.Id == id);
        }


        [HttpGet("GetAddress/{id}")]
        public async Task<IActionResult> GetAddress(int id)
        {
            var userId = _userManager.GetUserId(User);
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            var fullAddress = $"{address.Neighborhood} Mah. {address.StreetAddress}, {address.District}/{address.City}";

            return Json(new
            {
                fullAddress,
                address.PhoneNumber,
                address.FirstName,
                address.LastName,
                address.City,
                address.District,
                address.Neighborhood,
                address.StreetAddress,
                address.ZipCode
            });
        }
    }
}