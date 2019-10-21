using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Services;
using BTCPayServer.Storage.Models;
using BTCPayServer.Storage.Services.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace BTCPayServer.Storage.Services
{
    public class FileService
    {
        private readonly StoredFileRepository _FileRepository;
        private readonly IEnumerable<IStorageProviderService> _providers;
        private readonly SettingsRepository _SettingsRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        public FileService(StoredFileRepository fileRepository, IEnumerable<IStorageProviderService> providers,
            SettingsRepository settingsRepository, UserManager<ApplicationUser> userManager)
        {
            _FileRepository = fileRepository;
            _providers = providers;
            _SettingsRepository = settingsRepository;
            _userManager = userManager;
        }

        public async Task<StoredFile> AddFile(IFormFile file, string userId)
        {
            var settings = await _SettingsRepository.GetSettingAsync<StorageSettings>();
            var provider = GetProvider(settings);

            var storedFile = await provider.AddFile(file, settings);
            storedFile.ApplicationUserId = userId;
            await _FileRepository.AddFile(storedFile);
            return storedFile;
        }

        public async Task<string> GetFileUrl(Uri baseUri, string fileId, ClaimsPrincipal claimsPrincipal)
        {
            var settings = await _SettingsRepository.GetSettingAsync<StorageSettings>();
            var provider = GetProvider(settings);
            var storedFile = await _FileRepository.GetFile(fileId);
            await ThrowInvalidAccess(claimsPrincipal, storedFile);
            return storedFile == null ? null : await provider.GetFileUrl(baseUri, storedFile, settings);
        }

        public async Task<string> GetFilePath(string fileId, ClaimsPrincipal claimsPrincipal)
        {
            var settings = await _SettingsRepository.GetSettingAsync<StorageSettings>();
            var provider = GetProvider(settings);
            var storedFile = await _FileRepository.GetFile(fileId);
            await ThrowInvalidAccess(claimsPrincipal, storedFile);
            return storedFile == null ? null : await provider.GetFilePath(storedFile, settings);
        }

        public async Task<string> GetFileUrlByAccess(Uri baseUri, string fileId)
        {
            var settings = await _SettingsRepository.GetSettingAsync<StorageSettings>();
            var provider = GetProvider(settings);
            var storedFile = await _FileRepository.GetFile(fileId);
            return storedFile == null ? null : await provider.GetFileUrl(baseUri, storedFile, settings);
        }

        public async Task<string> GetTemporaryFileUrl(Uri baseUri, string fileId, DateTimeOffset expiry,
            bool isDownload)
        {
            var settings = await _SettingsRepository.GetSettingAsync<StorageSettings>();
            var provider = GetProvider(settings);
            var storedFile = await _FileRepository.GetFile(fileId);
            return storedFile == null ? null : await provider.GetTemporaryFileUrl(baseUri, storedFile, settings, expiry, isDownload);
        }

        public async Task RemoveFile(string fileId, string userId)
        {
            var settings = await _SettingsRepository.GetSettingAsync<StorageSettings>();
            var provider = GetProvider(settings);
            var storedFile = await _FileRepository.GetFile(fileId);
            if (string.IsNullOrEmpty(userId) ||
                storedFile.ApplicationUserId.Equals(userId, StringComparison.InvariantCultureIgnoreCase))
            {
                await provider.RemoveFile(storedFile, settings);
                await _FileRepository.RemoveFile(storedFile);
            }
        }

        private IStorageProviderService GetProvider(StorageSettings storageSettings)
        {
            return _providers.FirstOrDefault((service) => service.StorageProvider().Equals(storageSettings.Provider));
        }

        async Task ThrowInvalidAccess(ClaimsPrincipal claimsPrincipal, StoredFile storedFile)
        {
            //I have add it because user maybe upload its own picture and other user must not access it.TODO.Actually i must test other controller that access this method.
            //TODO.Make it better for performance and also make it generic
            var user = await _userManager.GetUserAsync(claimsPrincipal);
            if (!(string.IsNullOrWhiteSpace(storedFile.ApplicationUserId) ||
                (user != null && user.Id.Equals(storedFile.ApplicationUserId, StringComparison.InvariantCulture))
                 || (user != null && (await _userManager.IsInRoleAsync(user, Roles.ServerAdmin)))
                ))
            {
                throw new AccessViolationException("You do not have access to this file");
            }
        }
    }
}
