using System.Net.Mime;
using System.Text;
using System.Xml.Linq;

namespace Publicuem2
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
            return httpContext.Response.SendFileAsync(_fn);
        }
    }
}