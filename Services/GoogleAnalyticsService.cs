using Google.Apis.AnalyticsData.v1beta;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.AnalyticsData.v1beta.Data;

public interface IGoogleAnalyticsService
{
    List<(string Date, int Sessions)> GetDailyVisits(DateTime startDate, DateTime endDate);
}

public class GoogleAnalyticsService : IGoogleAnalyticsService
{
    private readonly string _propertyId;
    private readonly string _credentialPath;

    public GoogleAnalyticsService(string propertyId, string credentialPath)
    {
        _propertyId = propertyId;
        _credentialPath = credentialPath;
    }

    public List<(string Date, int Sessions)> GetDailyVisits(DateTime startDate, DateTime endDate)
    {
        GoogleCredential credential = GoogleCredential.FromFile(_credentialPath);

        var service = new AnalyticsDataService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential
        });

        var request = new RunReportRequest
        {
            Property = $"properties/{_propertyId}",
            Dimensions = new List<Dimension> 
            { 
                new Dimension { Name = "date" } 
            },
            Metrics = new List<Metric> 
            { 
                new Metric { Name = "sessions" } 
            },
            DateRanges = new List<DateRange> 
            { 
                new DateRange
                {
                    StartDate = startDate.ToString("yyyy-MM-dd"),
                    EndDate = endDate.ToString("yyyy-MM-dd")
                }
            }
        };

        RunReportResponse response = service.Properties.RunReport(request, $"properties/{_propertyId}").Execute();

        var results = new List<(string Date, int Sessions)>();
        foreach (var row in response.Rows)
        {
            var date = row.DimensionValues[0].Value;
            if (string.IsNullOrEmpty(date))
            {
                date = "Unknown";
            }
            var sessions = 0;
            if (int.TryParse(row.MetricValues[0].Value, out int parsedSessions))
            {
                sessions = parsedSessions;
            }
            results.Add((date, sessions));
        }

        return results;
    }
}