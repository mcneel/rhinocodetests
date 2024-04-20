//// r nuget "RestSharp==106.11.7"
#r "nuget: RestSharp, 106.11.7"

using System;
using System.Collections.Generic;
using Rhino;

using RestSharp;
using RestSharp.Authenticators;

// NOTE: https://restsharp.dev/getting-started/getting-started.html
// var client = new RestClient("https://api.twitter.com/1.1");
// client.Authenticator = new HttpBasicAuthenticator("username", "password");
// var request = new RestRequest("statuses/home_timeline.json", DataFormat.Json);
// var response = client.Get(request);

var client = new RestClient("https://httpbin.org");
var request = new RestRequest("get", DataFormat.Json);
var response = client.Get(request);

Console.WriteLine(response.Content);