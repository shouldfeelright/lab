using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class Script : ScriptBase
{
    private const string UserAgent = "TALXIS-Lab-WarehousePicking/1.0 (+https://github.com/TALXIS/lab)";

    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        switch (this.Context.OperationId)
        {
            case "GetProductByBarcode":
                return await GetProductByBarcode().ConfigureAwait(false);
            case "GetProductImage":
                return await GetProductImage().ConfigureAwait(false);
            default:
                return await this.Context.SendAsync(this.Context.Request, this.CancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<HttpResponseMessage> GetProductByBarcode()
    {
        this.Context.Request.Headers.Remove("User-Agent");
        this.Context.Request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);

        var response = await this.Context.SendAsync(this.Context.Request, this.CancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return response;
        }

        var body = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        if ((int?)body["status"] != 1)
        {
            return JsonResponse(HttpStatusCode.NotFound, new JObject { ["message"] = "No product found for this barcode" });
        }

        var product = body["product"] as JObject ?? new JObject();
        var flat = new JObject
        {
            ["ean"] = (string)body["code"],
            ["name"] = (string)product["product_name"],
            ["brand"] = (string)product["brands"],
            ["quantity"] = (string)product["quantity"],
            ["imageUrl"] = (string)product["image_url"]
        };
        response.Content = new StringContent(flat.ToString(), Encoding.UTF8, "application/json");
        return response;
    }

    private async Task<HttpResponseMessage> GetProductImage()
    {
        var query = System.Web.HttpUtility.ParseQueryString(this.Context.Request.RequestUri.Query);
        var imageUrl = query["imageUrl"];
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return JsonResponse(HttpStatusCode.BadRequest, new JObject { ["message"] = "imageUrl query parameter is required" });
        }

        var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        return await this.Context.SendAsync(request, this.CancellationToken).ConfigureAwait(false);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, JObject body)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
        };
    }
}
