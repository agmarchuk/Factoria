using System.Net.Mime;
using System.Text;
using System.Xml.Linq;

namespace Soran1957
{

    public class MimeResult : IResult
    {
        private readonly string _fn, _ct;

        public MimeResult(string fn, string ct)
        {
            _fn = fn;
            _ct = ct;
        }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            //string filename = "wwwroot/soldat.jpg";
            httpContext.Response.ContentType = _ct;

            //byte[] bytes = File.ReadAllBytes(filename);
            //httpContext.Response.ContentLength = bytes.LongLength;
            if (File.Exists(_fn))
            {
                return httpContext.Response.SendFileAsync(_fn);
            }
            else
            {
                return httpContext.Response.SendFileAsync("wwwroot/error.jpg");
            }
        }
    }
}