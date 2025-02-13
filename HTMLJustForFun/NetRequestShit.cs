﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsQuery;
using System.Net.Http.Headers;
using System.Runtime.Serialization;

namespace WebRequest
{


    /// <summary>
    /// A declared delegate for swapping out parts of the request class, 
    /// Just so we can modify the headers in custimizable ways before the 
    /// http request is being made. 
    /// </summary>
    /// <param name="headers">
    /// The headers in the httpclient,before the request is made. 
    /// </param>
    public delegate void CustomizedHeaders(HttpRequestHeaders headers);


    /// <summary>
    /// This is a class that can make get and post request, it's very useful. 
    /// </summary>
    /// <remarks>
    /// Makes headers, prepare cookies and make get and post request. 
    /// 
    /// </remarks>
    public class MyLittleRequest : IDisposable
    {
        // URL upon first request. 
        //public string base_uri;
        public CookieCollection cookie_jar { get; set; }
        public HttpClient client {get; protected set;}
        public HttpClientHandler client_handler { get; protected set;}
        //Swappable delegate.
        public CustomizedHeaders header_customizer;


        /// <summary>
        /// Use a baseful to construct an instance. 
        /// </summary>
        /// <param name="baseurl">
        /// URL with http/s protocol, it will reject if it's not start 
        /// with http / https
        /// </param>
        public MyLittleRequest(string baseurl)
        {
            //Varify Base Url.
            Regex rx = new Regex(@"^https?://.*$");
            Match m = rx.Match(baseurl);
            if (!m.Success)
            {
                throw new IncorrectURL();
            }
            //base_uri = baseurl;
            client_handler = new HttpClientHandler();
            client_handler.AllowAutoRedirect = true;
            client_handler.UseCookies = true;
            client_handler.MaxResponseHeadersLength = 30;
            client = new HttpClient(client_handler);
            
        }


        /// <summary>
        /// Chooses the function to prepare the header for a get/post 
        /// request. 
        /// <remarks>
        /// Called right before the time when the get/post? request is made. 
        /// </remarks>
        /// </summary>
        protected void SetupHeaderModifier()
        {
            if (this.header_customizer == null)
            {
                PrepareHeaders(client.DefaultRequestHeaders);
            }
            else
            {
                this.header_customizer(client.DefaultRequestHeaders);
            }
        }


        /// <summary>
        /// Prepare the headers for the http client. 
        /// </summary>
        protected void PrepareHeaders(HttpRequestHeaders header)
        {
            header.Add(
                "user-agent",
                "PostmanRuntime/7.15.0"
                );
            //header.Add(
            //    "accept",
            //    "*/*"
            //    );
            //Check Customed Headers. 
            header.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            
        }


        public async Task<HttpResponseMessage> MakeGetRequestAsync
        (
            string base_uri = "",
            IDictionary<string, string> Uri_Params = null
        )
        {
            SetupHeaderModifier();
            var p = Uri_Params == null ? "" : new FormUrlEncodedContent(Uri_Params).ToString();
            Uri uri = new Uri(base_uri + p);
            CookieCollection c = cookie_jar != null ? cookie_jar : new CookieCollection();
            
            //prepare cookie
            client_handler.CookieContainer.Add(c);
            HttpResponseMessage response = await client.GetAsync(uri);
            return response;
        }


        /// <summary>
        /// Make a post request to a certain URL. 
        /// </summary>
        /// <param name="formdata">
        /// The formdata for for post. 
        /// </param>
        /// <returns>
        /// The raw http response. 
        /// </returns>
        public async Task<HttpResponseMessage> MakePostRequestAsync
        (string url = "", IDictionary<string, string> formdata = null)
        {
            if (formdata == null)
            {
                formdata = new Dictionary<string, string>();
                formdata[""] = "";
            }
            FormUrlEncodedContent content = new FormUrlEncodedContent(formdata);
            HttpResponseMessage response = await client.PostAsync(url, content);
            return response;
        }

        override
        public string ToString()
        {
            var res = "-----MyLittleRequest-----";
            return res;
        }

        public void Dispose()
        {
            this.client.Dispose();
        }
    }

    [Serializable]
    internal class IncorrectURL : Exception
    {
        public IncorrectURL()
        {
        }

        public IncorrectURL(string message) : base(message)
        {
        }

        public IncorrectURL(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IncorrectURL(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public class WebPage
    {
        public string base_url;
       
        protected string state = "not loaded yet";

        public CQ doc_css { get; protected set; }
        // the request is going to be made when page loads. 
        public MyLittleRequest mlr_thispage { get; protected set;} 
        public CookieContainer cookie_pot { get; protected set; }

        protected HttpRequestHeaders request_headers;
        protected HttpResponseHeaders response_headers;
        public string content_raw_string;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseurl"></param>
        /// <param name="cookies"></param>
        public WebPage(string baseurl)
        {
            Regex rx = new Regex(@"^https?://.*$");
            Match m = rx.Match(baseurl);
            if (!m.Success)
            {
                throw new IncorrectURL();
            }

            base_url = baseurl;
            cookie_pot = new CookieContainer();
            mlr_thispage = new MyLittleRequest(base_url);
        }

        /// <summary>
        /// Make a GET request to the base url. 
        /// </summary>
        /// <returns>
        /// Tre or false to indication operations successful or not. 
        /// </returns>
        public void LoadPage()
        {
            MyLittleRequest mlr = mlr_thispage;
            mlr.cookie_jar = cookie_pot.GetCookies(new Uri(base_url));
            HttpResponseMessage res = mlr.MakeGetRequestAsync().Result;
            state = "loaded";
            string content_str = res.Content.ReadAsStringAsync().Result;
            content_raw_string = content_str;
            doc_css = new CQ(content_str);
            response_headers = res.Headers;
            this.request_headers = mlr.client.DefaultRequestHeaders;

            //store the bloody cookies;
            this.cookie_pot.Add
            (
                mlr.client_handler.CookieContainer.GetCookies(new Uri(this.base_url))
            );

            //refresh base url
        }


        /// <summary>
        /// Return an unloaded page that has cookies from this page. 
        /// <remarks>
        /// It only transfers the cookies, It adds this url's cookies 
        /// to the new url's cookies refering to the new url. 
        /// </remarks>
        /// 
        /// </summary>
        /// <param name="url">
        /// String, url that got transfer to, 
        /// Absolute URL please. 
        /// </param>
        /// <returns>
        /// An new unloaded instance of the Webpage object. 
        /// </returns>
        public WebPage TransferCookies(string url)
        {
            var newpage = new WebPage(url);
            // Cookies Transfer
            newpage.cookie_pot.Add
                (
                    new Uri(url) , 
                    this.cookie_pot.GetCookies(new Uri(this.base_url))
                );
            return newpage;
        }

        override
        public string ToString()
        {
            var nl = Environment.NewLine;
            var res = $"---------->Web Page: \"{base_url}\" ";
            if (this.state == "not loaded yet")
            {
                res += "is not loaded yet";
                return res;
            }
            res += "Is loaded"+nl;
            res += $"Request Header: {nl}" + this.request_headers.ToString() + nl;
            res += "Request Cookies Len: " + this.mlr_thispage.client_handler.CookieContainer.Count 
                + nl;
            res += $"Response Header: {nl}"+ this.response_headers.ToString() + nl;
            res += "Cookies Gotten: " + 
                this.cookie_pot.GetCookieHeader(new Uri(this.base_url)).ToString() + nl;
            res += "String Content Lenght: " + this.content_raw_string.Length;
            return res;
        }

        /// <summary>
        /// Gives the name of the header and it returns the value for that header.
        /// </summary>
        /// <param name="name">
        /// 
        /// The name of the key. 
        /// </param>
        /// <returns>
        /// null is returned if we can not find the value for that header. 
        /// </returns>
        public string GetValFromResponseHeader(string name)
        {
            IEnumerable<string> res = new List<string>();
            if (this.response_headers.TryGetValues(name, out res))
            {
                string result = "";
                foreach (var term in res)
                {
                    result += term + "; ";
                }
                return result;
            }
            return null;
        }
    }


    /// <summary>
    /// This class has a MyLittleRequest in it. 
    /// </summary>
    public class WebPage2
    {
        public string base_url;
        protected string state = "not loaded yet";
        public CQ doc_css { get; protected set; }
        // the request is going to be made when page loads. 
        // Feel free to access and modify this properties. 
        public MyLittleRequest mlr_thispage { get; protected set; }

        public HttpRequestHeaders request_headers { get; protected set; }
        public HttpResponseHeaders response_headers { get; protected set; }
        public string content_raw_string;

        public WebPage2(string baseurl)
        {
            Regex rx = new Regex(@"^https?://.*$");
            Match m = rx.Match(baseurl);
            if (!m.Success)
            {
                throw new IncorrectURL();
            }

            base_url = baseurl;
            // Instanciate request
            mlr_thispage = new MyLittleRequest(base_url);
        }
        public void LoadPage()
        {
            MyLittleRequest mlr = mlr_thispage;
            HttpResponseMessage res = mlr.MakeGetRequestAsync(this.base_url).Result;
            state = "loaded";
            string content_str = res.Content.ReadAsStringAsync().Result;
            content_raw_string = content_str;
            doc_css = new CQ(content_str);
            response_headers = res.Headers;
            this.request_headers = mlr.client.DefaultRequestHeaders;
        }

        /// <summary>
        /// This method will exchange the request object to the new instance of Webpage. 
        /// <remarks>
        /// The returned webpage is not loaded. 
        /// </remarks>
        /// </summary>
        /// <param name="url">
        /// string representing the url. 
        /// </param>
        /// <returns>
        /// An new instance of the the page. 
        /// </returns>
        public WebPage2 Transfer(string url)
        {
            WebPage2 newwebpage = new WebPage2(url);
            newwebpage.mlr_thispage = this.mlr_thispage;
            return newwebpage;
        }

        override
        public string ToString()
        {
            var nl = Environment.NewLine;
            var res = $"---------->Web Page: \"{base_url}\" ";
            if (this.state == "not loaded yet")
            {
                res += "is not loaded yet";
                return res;
            }
            res += "Is loaded" + nl;
            res += $"Request Header: {nl}" + this.request_headers.ToString() + nl;
            res += "Request Cookies Len: " + this.mlr_thispage.client_handler.CookieContainer.Count
                + nl;
            res += $"Response Header: {nl}" + this.response_headers.ToString() + nl;
            res += "Cookies len: " + this.mlr_thispage.client_handler.CookieContainer.Count+nl;
            res += "String Content Lenght: " + this.content_raw_string.Length;
            return res;
        }
    }

    [Obsolete]
    /// <summary>
    /// Saving session and relative info for a webpage. 
    /// </summary>
    public class MyLittleWebPage
    {
        public string base_url;
        protected string sate = "not yet loaded";
        public HtmlWeb web { get; protected set; }
        public HtmlDocument doc { get; protected set; }
        //Session
        public CookieContainer cookie_pot {get; protected set;}
        protected IDictionary<string, string> customized_header;
        protected Uri refer_url;
        protected string user_agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.90 Safari/537.36";

        protected HttpWebRequest request;
        protected HttpWebResponse post_reponse;

        public MyLittleWebPage(String baseurl)
        {
            base_url = baseurl;
        }



        /// <summary>
        /// Load the page.
        /// </summary>
        public void LoadPage()
        {
            try
            {
                web = new HtmlWeb();
                web.UseCookies = true;
                web.CaptureRedirect = true;
                web.PreRequest = new HtmlWeb.PreRequestHandler(PreRequestFiddling);
                web.PostResponse = new HtmlWeb.PostResponseHandler(PostResponseFiddling);
                doc = web.Load(base_url);
                sate = "loaded";
            }
            catch(System.Net.WebException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.Response);
                Console.WriteLine(e.Status);
                Console.WriteLine(e.Data);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(request == null?"null":request.Headers.ToString());
            }
        }




        /// <summary>
        /// Get the info after the web page responded. 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        void PostResponseFiddling(HttpWebRequest request,HttpWebResponse response)
        {
            cookie_pot = request.CookieContainer;
            this.request = request;
            post_reponse = response;
        }


        /// <summary>
        /// Prepare prerequest cookies. Prepare refered url. 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        bool PreRequestFiddling(HttpWebRequest request)
        {
            request.Headers.Clear();
            if(this.cookie_pot != null)request.CookieContainer = cookie_pot;
            if(this.refer_url != null)request.Headers.Add("referer", refer_url.AbsoluteUri);
            if(this.user_agent != null) request.UserAgent = this.user_agent;
            request.KeepAlive = false;
            request.Accept = 
                "text / html,application / xhtml + xml,application / xml; q = 0.9,image / webp,image / apng,*/*;q=0.8,application/signed-exchange;v=b3";
            request.Headers.Add("Upgrade-Insecure_requests", "1");
            this.request = request;
            
            return true;
        }


        override
        public string ToString()
        {
            if (sate != "not yet loaded")
            {
                
                var l = Environment.NewLine;
                var res = "";
                res += "Status code: " + this.post_reponse.Headers.Get("status");
                res += l;
                res += "Response Uri: " + web.ResponseUri;
                res += l;
                res += "Request Header: ---"+ l + request.Headers.ToString();
                res += l;
                res += "Response Header: ---" + l + post_reponse.Headers.ToString();
                res += l;
                res += "Cookies: " + l;
                if (cookie_pot != null)
                {
                    var collection = cookie_pot.GetCookies(web.ResponseUri);
                    foreach (Cookie c in collection)
                    {
                        res += c.Name + " = " + c.Value + "l";
                    }
                }
                //res += doc.Text;
                return res;
            }
            return "Page Link: " + base_url;
        }
    }

    /// <summary>
    /// A speicial mylittle webpage, it opens on deviant art page only. 
    /// </summary>
    public class DeviantArtPage : WebPage
    {
        internal DeviantArtPage(string url):base(url)
        {
        }

        /// <summary>
        /// Create an instance of this specific page. 
        /// </summary>
        /// <param name="dapagelink">
        /// A da link
        /// </param>
        /// <returns>
        /// Null if the page is not in the da domain.
        /// </returns>
        public static DeviantArtPage GetInstance(string dapagelink)
        {
            Regex rx = new Regex(@"^https://www\.deviantart.*$");
            var mc = rx.Match(dapagelink);
            bool success = mc.Success;
            return success?new DeviantArtPage(dapagelink):null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// Null is returned if the download link is not there. 
        /// The dl link is absolute.
        /// </returns>
        public string GetDownloadLink()
        {
            var l = this.doc_css["a.dev-page-download[href]"];
            return l.Attr("href");
        }


        /// <summary>
        /// This function cusomized the headers for a new request and transfer 
        /// cookies to the new download url. 
        /// </summary>
        /// <returns>
        /// Null if returned if there is something wrong
        /// </returns>
        public WebPage Download_Redirect()
        {
            string dl = this.GetDownloadLink();
            if (dl.Length != 0)
            {
                WebPage newpage = this.TransferCookies(dl);
                //Establish Headers. 
                CustomizedHeaders header_modifier = delegate(HttpRequestHeaders header)
                {
                    header.Add
                        (
                           "user-agent",
                           "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML," +
                           " like Gecko) Chrome/75.0.3770.90 Safari/537.36"
                        );
                    header.Add
                        (
                            "accept",
                            "text/html,application/xhtml+xml,application/xml;q=0.9," +
                            "image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3"
                        );
                    header.Add
                        (
                            "accept-encoding",
                            "gzip, deflate, br"
                        );
                    header.Add
                        (
                          "referer",
                          base_url
                        );
                    header.Add
                        (
                            "upgrade-insecure-requests", "1"
                        );
                    header.Add
                        (
                            "host", "www.deviantart.com"
                        );
                };
                newpage.mlr_thispage.header_customizer = header_modifier;
                return newpage;
            }
            return null;
        }



    }

    [Obsolete]
    /// <summary>
    /// Creates a class for making api calls.
    /// </summary>
    public class MyLittleRequest1
    {
        // URL upon first request. 
        public string base_uri;
        public CookieCollection cookie_jar;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url">
        /// The extra parameters that should be appeneded 
        /// at the end of the url.
        /// </param>
        public MyLittleRequest1(string uri)
        {
            this.base_uri = uri;
        }

        /// <summary>
        /// Give params appeneded to url, it will make a get request. 
        /// </summary>
        /// <remarks>
        ///     Method Tested Casually
        /// </remarks>
        /// <param name="Url_Params">
        /// String, representing the part at the end of the URL. 
        /// </param>
        /// <returns>
        /// A Http ResponseMessage for you to process. 
        /// </returns>
        public async Task<HttpResponseMessage> MakeGetRequestAsync
        (
            IDictionary<string, string> Uri_Params = null
        )
        {
            var p = Uri_Params == null ? "" : new FormUrlEncodedContent(Uri_Params).ToString();
            Uri uri = new Uri(this.base_uri + p);
            HttpClientHandler handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            CookieContainer c = new CookieContainer();

            //prepare cookie
            if (this.cookie_jar != null)
            {
                // foreach (KeyValuePair<string, string> kvp in this.cookie_jar)
                // {
                //     c.Add(uri, new Cookie(kvp.Key, kvp.Value));
                // }
                handler.CookieContainer.Add(this.cookie_jar);
            }

            //prepare http client
            using (HttpClient client = new HttpClient(handler))
            {
                //client headers
                PrepareHeader(client.DefaultRequestHeaders);
                HttpResponseMessage response = await client.GetAsync(uri);
                return response;
            }
        }


        /// <summary>
        /// An internal method, used for preparing http headers for get and post request. 
        /// </summary>
        protected void PrepareHeader(HttpRequestHeaders header)
        {
            header.Add(
                "user-agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.90 Safari/537.36"
                );
            header.Add(
                "accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3"
                );
        }

        /// <summary>
        /// A method that makes a mpost request to the base url in the class. 
        /// </summary>
        /// <remarks>
        /// Method Casually Tested. 
        /// </remarks>
        /// <param name="formdata">
        /// A Dictionary representing the formdata you want to upload for the requst. 
        /// </param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> MakePostRequestAsync
        (IDictionary<string, string> formdata)
        {
            if (formdata == null)
            {
                formdata = new Dictionary<string, string>();
                formdata[""] = "";
            }

            string url = this.base_uri;
            using (HttpClient client = new HttpClient())
            {
                FormUrlEncodedContent content = new FormUrlEncodedContent(formdata);
                HttpResponseMessage response = await client.PostAsync(url, content);
                return response;
            }
        }
    }
}
/*  Log: 
 *  The reponse made by deviant art transfer did not contain content-type 
 *  in the response header. 
 * 
 * 
 * 
 */