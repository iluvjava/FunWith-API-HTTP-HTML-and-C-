# FunWith-API-HTTP-HTML-and-C-
Just for fun. It's either this or math analysis textbook.
# What is this: 
I am using c# net framework to make http request calls and stuff like that. 

# Developer Log: 17/06/2019:
```
    Question: 
        Does C# httpclient save cookies automatically from the response header of the request? 
            - Yeah, the cookies is saved under the HttpClientHandler that used to initiate the HttpClient. It auto sets
            cookies if the given response has "set cookies" properties in the header. 
        What are the list of Request Properties httpclinet supports? 
            - By default, there is no properties in the HttpClientHandler.
```