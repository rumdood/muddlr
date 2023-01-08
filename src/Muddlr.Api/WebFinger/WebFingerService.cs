using System.Text.Json;
using System.Text.Json.Serialization;
using Muddlr.WebFinger;

namespace Muddlr.Api;

public class WebFingerService : IWebFingerService
{
    private readonly ILogger<WebFingerService> _logger;

    private const string DataRoot = ".data";
    private const string AccountLocators = "account_locators.json";
    private const string WebFingerFolder = "WebFinger";
    private const string FaultFolder = "Fault";
    
    private readonly string _folder;
    private readonly string _faultedFolder;
    
    // index of webfinger records by filename
    private readonly Dictionary<string, WebFingerRecord> _webFingerCacheByAccount = new();
    private readonly Dictionary<string, WebFingerRecord> _webFingerByLocator = new();
    private readonly Dictionary<string, FediverseAccountWithLocators> _accountsWithLocators = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private void LoadWebFingerRecordsCache()
    {
        if (!Directory.Exists(_folder))
        {
            Directory.CreateDirectory(_folder);
        }

        var faulted = new List<string>();

        var jsonFiles = Directory.EnumerateFiles(_folder, "*.json", SearchOption.TopDirectoryOnly).ToArray();

        foreach (var file in jsonFiles)
        {
            if (TryGetRecordFromFile(file, out var record) && record is not null)
            {
                // update the cache
                _webFingerCacheByAccount[Path.GetFileName(file)] = record;
                continue;
            }
            
            _logger.LogError("Failed to index WebFinger Record in {F}", file);
            faulted.Add(file);
        }

        foreach (var file in faulted)
        {
            File.Move(file, Path.Combine(_faultedFolder, Path.GetFileName(file)));
        }

        /*
        foreach (var file in faulted.Select(x => new FileInfo(x)).OrderByDescending(f => f.LinkTarget ?? ""))
        {
            if (!string.IsNullOrEmpty(file.LinkTarget))
            {
                file.Delete();
                continue;
            }
            
            file.MoveTo(Path.Combine(_faultedFolder, file.Name));
        }
        */
    }

    private List<FediverseAccountWithLocators> GetAccountsAndLocators()
    {
        var locatorFile = Path.Combine(DataRoot, AccountLocators);

        if (!File.Exists(locatorFile))
        {
            return new List<FediverseAccountWithLocators>();
        }

        var json = File.ReadAllText(locatorFile);
        return JsonSerializer.Deserialize<FediverseAccountWithLocators[]>(json, JsonOptions).EmptyIfNull().ToList();
    }

    private void LoadLocatorsCache()
    {
        foreach (var account in _accountsWithLocators.Values)
        {
            if (!TryGetRecordForAccount(account.Account, out var record) || record is null)
            {
                continue; // ?
            }

            foreach (var locator in account.Locators)
            {
                _webFingerByLocator[locator] = record;
            }
        }
    }

    private async Task SaveWebFingerRecord(FediverseAccount targetAccount, IEnumerable<string> locators, WebFingerRecord record)
    {
        var fileName = GetFileNameForAccount(targetAccount);
        var filePath = Path.Combine(_folder, fileName);

        await using var stream = new FileStream(filePath, FileMode.OpenOrCreate);
        await JsonSerializer.SerializeAsync(stream, record, JsonOptions);

        if (!_accountsWithLocators.TryGetValue(targetAccount.Key, out var acct))
        {
            acct = new FediverseAccountWithLocators {Account = targetAccount};
        }

        acct.Locators.Clear();

        foreach (var locator in locators)
        {
            /*
            var locatorFileName = GetFileNameForLocator(locator);
            File.CreateSymbolicLink(Path.Combine(_folder, locatorFileName), filePath);
            */
            var corrected = locator.StartsWith("acct:") ? locator : $"acct:{locator}";
            acct.Locators.Add(corrected);
            _webFingerByLocator[corrected] = record;
        }

        _accountsWithLocators[acct.Account.Key] = acct;

        await SaveLocators();
    }
    
    private static HashSet<Uri> GenerateAliases(FediverseAccount account)
    {
        return new HashSet<Uri>
        {
            new Uri($"https://{account.FediverseServer}/@{account.Username}"),
            new Uri($"https://{account.FediverseServer}/users/{account.Username}")
        };
    }

    private static List<WebFingerLink> GenerateFediverseLinks(FediverseAccount account)
    {
        return new List<WebFingerLink>
        {
            new WebFingerLink
            {
                Relationship = Relationship.WebFingerProfile,
                Type = LinkType.TextHtml,
                Href = new Uri($"https://{account.FediverseServer}/@{account.Username}")
            },
            new WebFingerLink
            {
                Relationship = Relationship.Self,
                Type = LinkType.ApplicationActivityJson,
                Href = new Uri($"https://{account.FediverseServer}/users/{account.Username}")
            },
            new WebFingerLink
            {
                Relationship = Relationship.OStatusSubscribe,
                Template = $"https://{account.FediverseServer}/authorize_interaction?uri={{uri}}"
            },
        };
    }

    private static string GetFileNameForAccount(FediverseAccount account) => $"{account.Key}.json";

    // private static string GetFileNameForLocator(string locator) => $"LOC_{locator}.json";

    private bool FileExistsForRecord(FediverseAccount account) => FileExists(GetFileNameForAccount(account));

    private bool FileExists(string fileName)
    {
        return File.Exists(Path.Combine(_folder, fileName));
    }

    private bool TryGetRecordFromFile(string fileName, out WebFingerRecord? record)
    {
        var filePath = Path.Combine(_folder, fileName);
        
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            record = JsonSerializer.Deserialize<WebFingerRecord>(json, JsonOptions);
            return record is not null;
        }

        record = null;
        return false;
    }

    private bool TryGetRecordForAccount(FediverseAccount account, out WebFingerRecord? record)
    {
        if (!_webFingerCacheByAccount.TryGetValue(account.Key, out var existing))
        {
            var fileName = GetFileNameForAccount(account);
            // couldn't find the existing record in the cache, check to see if it's on the filesystem
            if (!TryGetRecordFromFile(fileName, out existing) || existing is null)
            {
                record = null;
                return false;
            }

            // a cache miss - put it back in the cache
            _webFingerCacheByAccount[account.Key] = existing;
        }

        record = existing;
        return true;
    }

    private bool TryGetRecordForLocator(string locator, out WebFingerRecord? record)
    {
        if (_webFingerByLocator.TryGetValue(locator, out record))
        {
            return true;
        }

        var account = _accountsWithLocators.Values
            .SingleOrDefault(acct => acct.Locators.Contains(locator));

        if (account is null)
        {
            return false;
        }

        if (!_webFingerCacheByAccount.TryGetValue(account.Account.Key, out record))
        {
            var fileName = GetFileNameForAccount(account.Account);
            // couldn't find the existing record in the cache, check to see if it's on the filesystem
            if (!TryGetRecordFromFile(fileName, out record) || record is null)
            {
                return false;
            }

            // a cache miss - put it back in the cache
            _webFingerCacheByAccount[account.Account.Key] = record;
        }
        
        // a locator cache miss
        _webFingerByLocator[locator] = record;
        return true;
    }
    
    private async Task SaveLocators()
    {
        var fileName = Path.Combine(DataRoot, AccountLocators);
        await using var stream = File.Open(fileName, FileMode.OpenOrCreate);
        await JsonSerializer.SerializeAsync(stream, _accountsWithLocators.Values, JsonOptions);
    }

    public WebFingerService(ILogger<WebFingerService> logger, IWebHostEnvironment env)
    {
        if (env is null)
        {
            throw new ArgumentNullException(nameof(env));
        }
        
        _logger = logger;
        
        _folder = Path.Combine(env.ContentRootPath, DataRoot, WebFingerFolder);
        _faultedFolder = Path.Combine(_folder, FaultFolder);
        _accountsWithLocators = GetAccountsAndLocators().ToDictionary(kv => kv.Account.Key);
        LoadWebFingerRecordsCache();
        LoadLocatorsCache();
    }

    public async Task<WebFingerRecord> AddWebFingerRecord(WebFingerUpdateRequest request)
    {
        if (_webFingerCacheByAccount.TryGetValue(request.Account.Key, out _))
        {
            throw new InvalidOperationException("Cannot add record. Record already exists for account");
        }

        if (request.Locators.Any(loc => _webFingerByLocator.ContainsKey(loc.StartsWith("acct:") ? loc : $"acct:{loc}")))
        {
            throw new InvalidOperationException("Cannot add record. Duplicate locator exists");
        }

        var links = GenerateFediverseLinks(request.Account);
        var aliases = GenerateAliases(request.Account);

        var record = new WebFingerRecord
        {
            Subject = $"acct:{request.Account.Username}@{request.Account.FediverseServer}",
            Aliases = aliases,
            Links = links,
        };

        await SaveWebFingerRecord(request.Account, request.Locators, record);
        return record;
    }
    
    public async Task<WebFingerRecord> UpdateWebFingerRecord(WebFingerUpdateRequest request)
    {
        if (!TryGetRecordForAccount(request.Account, out var existing) || existing is null)
        {
            throw new InvalidOperationException($"Could not locate file for account [{request.Account.Key}]");
        }

        var newAliases = GenerateAliases(request.Account);
        var newLinks = GenerateFediverseLinks(request.Account);
        
        var record = new WebFingerRecord
        {
            Subject = $"acct:{request.Account.Username}@{request.Account.FediverseServer}",
            Aliases = newAliases,
            Links = newLinks,
        };

        await SaveWebFingerRecord(request.Account, request.Locators, record);
        return record;
    }

    public async Task<bool> AddLocator(string locator, FediverseAccount account)
    {
        if (_webFingerByLocator.ContainsKey(locator))
        {
            return false;
        }

        if (!_accountsWithLocators.TryGetValue(account.Key, out var acct))
        {
            return false;
        }

        acct.Locators.Add(locator);
        await SaveLocators();

        if (TryGetRecordForAccount(account, out var record) && record is not null)
        {
            _webFingerByLocator[locator] = record;
        }

        return true;

        /*
        var fileName = Path.Combine(_folder, GetFileNameForAccount(account));

        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"Could not locate file for account {fileName}");
        }

        var locatorFileName = Path.Combine(_folder, GetFileNameForLocator(locator));
        if (File.Exists(locatorFileName))
        {
            return false;
        }
        
        File.CreateSymbolicLink(locatorFileName, fileName);
        return true;
        */
    }

    public async Task<bool> RemoveLocator(string locator, FediverseAccount account)
    {
        if (!_webFingerByLocator.ContainsKey(locator))
        {
            return false;
        }

        if (!_accountsWithLocators.TryGetValue(account.Key, out var acct))
        {
            return false;
        }

        acct.Locators.Remove(locator);
        await SaveLocators();

        return _webFingerByLocator.Remove(locator);
        /*
        var fileName = Path.Combine(_folder, GetFileNameForAccount(account));

        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"Could not locate file for account {fileName}");
        }

        var locatorFileName = Path.Combine(_folder, GetFileNameForLocator(locator));
        if (!File.Exists(locatorFileName))
        {
            return false;
        }
        
        File.Delete(locatorFileName);
        return true;
        */
    }

    public async Task<bool> DeleteWebFingerRecord(FediverseAccount account)
    {
        var fileName = Path.Combine(_folder, GetFileNameForAccount(account));
        /*
        var locators = Directory.EnumerateFiles(_folder, "LOC_*.json", SearchOption.TopDirectoryOnly)
            .Where(file => File.ResolveLinkTarget(file, false) is {FullName: var fullName} &&
                           fullName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var locator in locators)
        {
            if (!File.Exists(locator))
            {
                continue;
            }

            File.Delete(locator);
        }
        */

        if (!File.Exists(fileName))
        {
            return false;
        }
        
        File.Delete(fileName);

        if (_accountsWithLocators.TryGetValue(account.Key, out var acct))
        {
            foreach (var locator in acct.Locators)
            {
                _ = _webFingerByLocator.Remove(locator);
            }

            _ = _accountsWithLocators.Remove(account.Key);
        }

        await SaveLocators();
        
        return true;
    }

    public async Task<WebFingerRecord> AddLinks(FediverseAccount account, IEnumerable<WebFingerLink> links)
    {
        throw new NotImplementedException();
    }

    public async Task<WebFingerRecord> RemoveLinks(FediverseAccount account, IEnumerable<WebFingerLink> links)
    {
        throw new NotImplementedException();
    }

    public async Task<WebFingerRecord?> GetWebFingerRecord(string locator)
    {
        return TryGetRecordForLocator(locator, out var record) ? record : null;
    }
}