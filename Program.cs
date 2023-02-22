using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using scraper.salamnews.org;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection.PortableExecutable;
using System.Security.Policy;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

Dictionary<string, string> translateDate = new();
Dictionary<string, string> categories = new();
translateDate.Add("Yanvar", "January");
translateDate.Add("Январь", "January");
translateDate.Add("Dekabr", "December");
translateDate.Add("Декабрь", "December");
string baseUrl = "http://web.archive.org";
List<Read> rdl = new();
SocketsHttpHandler webHandler = new();
HttpClient client = new(webHandler);
HashSet<string> hs = new();
var basePathX = AppDomain.CurrentDomain.BaseDirectory;
var finalPathL = Path.Combine(basePathX, "salamnews.links");
var finalPathX = Path.Combine(basePathX, "salamnews.data");
var finalPathXS = Path.Combine(basePathX, "connection.conf");
string con = con = File.ReadAllText($"{finalPathXS}");
Queue<string> queue = new Queue<string>();
Queue<string> snapshotsQ = new Queue<string>();
List<string> snapshots = new List<string>();
HashSet<string> visited = new() { "javascript:void(0)", "", "#", "/" };
int ru = 0;
int az = 0;
int n = 0;
int falt = 0;
int iiii = 0;
DateTime minDate = new DateTime(2010, 1, 1);
DateTime maxDate = new DateTime(2020, 1, 1);
Console.WriteLine(finalPathX);

void confHandlers()
{

    webHandler.AllowAutoRedirect = true;
    webHandler.ConnectTimeout = TimeSpan.FromMilliseconds(15 * 1000);
    webHandler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always;
    webHandler.UseCookies = true;
    webHandler.SslOptions.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls;
    client = new(webHandler);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.UserAgent.Clear();
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/6.0 (Windows NT 10.0; Win64; x64; rv:108.0) Gecko/20100101 Firefox/108.0");
    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
    client.DefaultRequestHeaders.Add("Connection", "keep-alive");
    client.DefaultRequestHeaders.Add("Host", "salamnews.org");
}
void dataToMysql()
{
    foreach (var line in File.ReadLines($"{finalPathX}"))
    {
        try
        {
            var data = line.Split(';');
            n++;
            saveMysql(data);
        }
        catch (Exception e)
        {
            _ = e;
        }
    }
}
void saveMysql(string[] data)
{
    try
    {
        //19 Январь 2023, 12:57(GMT + 4)
        string date = data[3]?.Split("(")[0] ?? "";
        foreach (var d in translateDate)
        {
            if (date.Contains(d.Key))
            {
                date = date.Replace(d.Key, d.Value);
                break;
            }
        }
        DateTime dt = DateTime.Parse(date);
        if (dt <= minDate || dt >= maxDate)
        {
            return;
        }

        MySqlConnection conn = new MySqlConnection(con);
        conn.Open();

        string uri = data[0] ?? "";
        //if (uri == "") return;
        string lang = uri.Contains("/ru/") ? "ru" : "az";
        if (lang == "ru") ru++;
        else az++;
        string category = data[1]?.Split("»")[1]?.Trim() ?? "";
        ////if (category == "") return;
        MySqlCommand comm = conn.CreateCommand();
        MySqlDataReader msdr;
        if (!categories.ContainsKey(category))
        {
            comm.CommandText = "SELECT id FROM `cbt_catnews_data` where name=@cate";
            comm.Parameters.AddWithValue("@cate", category);
            msdr = comm.ExecuteReader();
            category = "";
            if (msdr.Read())
            {
                category = msdr.GetString(0) ?? "";
            }
            msdr.Close();
        }
        else
        {
            category = categories[category];
        }

        if (string.IsNullOrEmpty(category))
        {
            //      return;
        }
        string title = data[2] ?? "";
        if (title == "")
        {
            return;
        }
        string body = data[4] ?? "";
        string image = "";
        try
        {
            image = data[5] ?? "";
            string dirname = image.Split("/")[1];
            string filename = image.Split("/")[2];
            comm = conn.CreateCommand();
            comm.CommandText = "SELECT id,date FROM `cbt_img` where dirname=@dirname and filename=@filename";
            comm.Parameters.AddWithValue("@dirname", dirname);
            comm.Parameters.AddWithValue("@filename", filename);
            msdr = comm.ExecuteReader();
            image = "";
            if (msdr.Read())
            {
                image = msdr.GetString(0) ?? "";
                //dt = msdr.GetDateTime(1);
            }
            msdr.Close();
        }
        catch (Exception e)
        {
            var basxePath = AppDomain.CurrentDomain.BaseDirectory;
            var finxalPath = Path.Combine(basxePath, "salamnews.fualt");
            StreamWriter scw = new StreamWriter(finxalPath, true);
            scw.WriteLine($"{uri.Trim()}");
            scw.Flush();
            scw.Close();
            falt++;
            //  return;
        }

        if (string.IsNullOrEmpty(image))
        {
            //  return;
        }
        //2010-11-01 22:11:33
        //date = dt.ToShortDateString();
        string id = uri.Split("/").Last();
        comm = conn.CreateCommand();
        comm.CommandText = "REPLACE INTO new_news(id,uid,strani,cate,date,images,opublic,lang,title,text,describ) VALUES(@id,54,17,@cate,@date,@images,@opublic,@lang,@title,@text,@describ)";
        comm.Parameters.AddWithValue("@id", id);
        comm.Parameters.AddWithValue("@cate", category);
        comm.Parameters.AddWithValue("@date", dt);
        comm.Parameters.AddWithValue("@images", image);
        comm.Parameters.AddWithValue("@opublic", "1");
        comm.Parameters.AddWithValue("@lang", lang);
        comm.Parameters.AddWithValue("@title", title);
        comm.Parameters.AddWithValue("@text", body);
        comm.Parameters.AddWithValue("@describ", title);
        comm.ExecuteNonQuery();
        conn.Close();
        iiii++;
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var finalPath = Path.Combine(basePath, "salamnews.saved");
        StreamWriter sw = new StreamWriter(finalPath, true);
        sw.WriteLine($"{uri.Trim()}");
        sw.Flush();
        sw.Close();
        logSaved(uri);
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
}
void updateDate()
{
    try
    {
        MySqlConnection conn = new MySqlConnection(con);
        conn.Open();
        MySqlCommand comm = conn.CreateCommand();
        comm.CommandText = "select id,lang, date from new_news";
        MySqlDataReader msdr;
        msdr = comm.ExecuteReader();
        while (msdr.Read())
        {
            try
            {
                string uri = msdr.GetString(0);
                string lang = msdr.GetString(1);
                DateTime dt = msdr.GetDateTime(2);

                if (!(dt <= minDate || dt >= maxDate)) continue;
                // if (lang == "ru") continue; //<---- remove this
                uri = $"http://web.archive.org/web/http://salamnews.org/{lang}/news/read/{uri}";
                bool ck = getUri(uri);
                if (!ck) Console.Write(uri);
                n++;
            }
            catch (Exception e)
            {
                _ = e.Message;
            }
        }
        msdr.Close();
    }
    catch (Exception e)
    {
        _ = e.Message;
    }
}
void updateCate()
{
    try
    {
        MySqlConnection conn = new MySqlConnection(con);
        conn.Open();
        MySqlCommand comm = conn.CreateCommand();
        comm.CommandText = "select uri from scrap_new_news";
        MySqlDataReader msdr;
        msdr = comm.ExecuteReader();
        while (msdr.Read())
        {
            try
            {
                string uri = msdr.GetString(0);
                bool ck = getUri(uri);
                if (!ck) Console.Write(uri);
                n++;
            }
            catch (Exception e)
            {
                _ = e.Message;
            }
        }
        msdr.Close();
    }
    catch { }
}
bool getUri(string uri)
{
    HttpResponseMessage response;
    try
    {
        response = client.GetAsync(uri).Result;
    }
    catch (Exception e)
    {
        _ = e.Message;
        return false;
    }
    if (response is null) return false;
    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine(response.StatusCode);
        return false;
    }
    Stream stream = response.Content.ReadAsStreamAsync().Result;
    savePageData(stream, uri, "salamnews.range.data", true);
    return true;
}
void mysqlToMysql()
{
    try
    {
        MySqlConnection conn = new MySqlConnection(con);
        conn.Open();
        MySqlCommand comm = conn.CreateCommand();
        comm.CommandText = "select uri, cate, date, image, lang, body, title from scrap_new_news";
        MySqlDataReader msdr;
        msdr = comm.ExecuteReader();
        while (msdr.Read())
        {
            try
            {
                string[] data = { msdr.GetString(0), msdr.GetString(1), msdr.GetString(6), msdr.GetString(2), msdr.GetString(5), msdr.GetString(3), };
                saveMysql(data);
                n++;
            }
            catch (Exception e)
            {
                _ = e.Message;
            }
        }
        msdr.Close();
    }
    catch (Exception e)
    {
        _ = e.Message;
    }
}
void logSaved(string uri)
{
    Console.Clear();
    Console.WriteLine($"AZ Articels: {az}");
    Console.WriteLine($"RU Articels: {ru}");
    Console.WriteLine($"Total Loaded: {n}");
    Console.WriteLine($"Total Saved: {n + iiii}");
    Console.WriteLine($"Total Failed: {falt}");
    Console.WriteLine($"Total Loaded Queued: {queue.Count()}");
    Console.WriteLine($"Total Snapshots: {snapshotsQ.Count()}");
    Console.WriteLine((uri.Split("/").LastOrDefault() ?? "") + " Saved. with no error");
}
void Crowl(string snapshot)
{
    snapshotsQ.Clear();
    snapshotsQ.Enqueue(snapshot);
    queue.Clear();
    Dictionary<string, int> sstry = new();

    while (snapshotsQ.Count > 0)
    {
        string currSnap = snapshot; //.Split("/")[2];
        string baseS = $"{snapshotsQ.Dequeue()}";
        queue.Enqueue($"{baseS}");
        while (queue.Count > 0)
        {
            Console.Title = $"Q:{queue.Count()} SQ:{snapshotsQ.Count()} C:{iiii}";
            string uri = queue.Dequeue();
            uri = $"{baseUrl}{uri}";
            uri = uri.Replace(baseUrl + baseUrl, baseUrl);
            if (visited.Contains(uri))
            {
                Console.WriteLine($"Already Visited");
                continue;
            }
            HttpResponseMessage response = client.GetAsync(uri).Result;
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("404 not found");
                    continue;
                }
                if (sstry.ContainsKey(uri))
                {
                    if (sstry[uri] > 10)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                        {
                            Console.WriteLine("Check your internet.");
                            Task.Delay(1000);
                            Console.WriteLine($"Retry {sstry[uri]}");
                            if (!queue.Contains(uri)) queue.Enqueue(uri);
                            continue;
                        }
                        Console.WriteLine($"{uri} Skipped");
                        continue;
                    }
                    sstry[uri]++;
                }
                else
                {
                    sstry.Add(uri, 0);
                }
                Console.WriteLine($"Retry {sstry[uri]}");
                if (!queue.Contains(uri)) queue.Enqueue(uri);
                continue;
            }
            visited.Add(uri);
            if (uri.Contains("news/read"))
            {
                Stream stream = response.Content.ReadAsStreamAsync().Result;
                savePageData(stream, uri);
                logSaved(uri);
            }
            else
            {
                Stream stream = response.Content.ReadAsStreamAsync().Result;
                string[] lnks = allLinks(stream);
                Queue<string> tmp = new();
                int b = queue.Count;
                foreach (var i in lnks)
                {
                    if (!i.Contains(currSnap))
                    {
                        //if (!snapshotsQ.Contains(i)) snapshotsQ.Enqueue(i);
                        continue;
                    }
                    if (!i.Contains("news/read"))
                    {
                        if (!tmp.Contains(i)) tmp.Enqueue(i);
                        continue;
                    }
                    if (!queue.Contains(i)) queue.Enqueue(i);
                }
                while (tmp.Count > 0)
                {
                    var j = tmp.Dequeue();
                    if (!queue.Contains(j)) queue.Enqueue(j);
                }
                Console.WriteLine($"{queue.Count() - b} New links added.");
            }
        }
    }
}
string[] allLinks(Stream stream)
{
    HtmlDocument doc = new HtmlDocument();
    doc.Load(stream);
    var lst = doc.DocumentNode.SelectNodes("//body//table//a");
    List<string> r = new();
    if (lst is not null)
    {
        foreach (var i in lst)
        {
            string ii = i.GetAttributeValue("href", "");
            if (queue.Contains(ii)) continue;
            if (!ii.Contains("salamnews.org")) continue;
            if (ii.StartsWith("#")) continue;
            if (visited.Contains(ii)) continue;
            if (!r.Contains(ii))
            {
                r.Add(ii); 
                StreamWriter sw = new StreamWriter(finalPathL, true);
                sw.WriteLine($"{ii.Trim()}");
                sw.Flush();
                sw.Close();
            }
        }
    }
    return r.ToArray();
}
string processUri(string uri)
{
    try
    {
        uri = uri.Replace("//web.archive.org", string.Empty);
        uri = uri.Replace("http://web.archive.org", string.Empty);
        uri = uri.Replace("://web.archive.org", string.Empty);
        uri = uri.Replace("https://web.archive.org", string.Empty);
        HttpResponseMessage response = client.GetAsync($"{baseUrl}{uri}").Result;
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(response.StatusCode);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return "";
            }
            return uri;
        }
        if (visited.Contains(uri)) return "";
        visited.Add(uri);
        if (uri.Contains("news/read"))
        {
            Stream stream = response.Content.ReadAsStreamAsync().Result;
            savePageData(stream, uri);
        }
    }
    catch (Exception e)
    {
        _ = e;
    }
    return "";
}
void saveScrapToMysql(string uri, string category, string dt, string image, string lang, string body, string title)
{
    MySqlConnection conn = new MySqlConnection(con);
    conn.Open();
    MySqlCommand comm = conn.CreateCommand();
    comm.CommandText = "REPLACE INTO scrap_new_news(uri, cate, date, image, lang, body, title) VALUES(@uri,@cate,@date,@image,@lang,@body,@title)";
    comm.Parameters.AddWithValue("@uri", uri);
    comm.Parameters.AddWithValue("@cate", category);
    comm.Parameters.AddWithValue("@date", dt);
    comm.Parameters.AddWithValue("@image", image);
    comm.Parameters.AddWithValue("@lang", lang);
    comm.Parameters.AddWithValue("@body", body);
    comm.Parameters.AddWithValue("@title", title);
    comm.ExecuteNonQuery();
    conn.Close();
    hs.Add(uri.Split("/").Last() ?? "");
}
void savePageData(Stream stream, string uri, string SavePath = "salamnews.data", bool mysql = false)
{
    HtmlDocument doc = new HtmlDocument();
    doc.Load(stream);
    try
    {
        if (doc.DocumentNode.InnerHtml.Contains("Wayback Machine has not archived that URL."))
        {
            return;
        }
        string Title = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText ?? "";
        if (Title == "")
        {
            return;
        }
        string Category = doc.DocumentNode.SelectSingleNode("//td[@class='nrt-top']//div[1]")?.InnerText ?? "";
        if (Category == "")
        {
            return;
        }
        string Date = doc.DocumentNode.SelectSingleNode("//span[@class='date']")?.InnerText ?? "";
        if (Date == "")
        {
            return;
        }
        Date = Date.Split("(")[0];
        bool pas = false;
        foreach (var d in translateDate)
        {
            if (Date.Contains(d.Key))
            {
                Date = Date.Replace(d.Key, d.Value);
                pas = true;
                break;
            }
        }
        if (!pas) return;
        DateTime dt = DateTime.Parse(Date);
        if (dt <= minDate || dt >= maxDate) return;
        string Image = doc.DocumentNode.SelectSingleNode("//div[@data-fit='contain']//img[1]")?.GetAttributeValue("src", "") ?? "";
        if (Image == "")
        {
            return;
        }
        Image = Image.Split("img.salamnews.org")[1];
        string Body = "";
        HtmlNodeCollection lst = doc.DocumentNode.SelectNodes("//div[@class='fotorama']/following-sibling::p");
        if (!(lst is null || lst?.Count == 0))
        {
            foreach (HtmlNode link in lst)
            {
                Body += link.OuterHtml;
            }
        }
        uri = uri.Replace(System.Environment.NewLine, "");
        Category = Category.Replace(System.Environment.NewLine, "");
        Title = Title.Replace(System.Environment.NewLine, "");
        Date = Date.Replace(System.Environment.NewLine, "");
        Body = Body.Replace(System.Environment.NewLine, "");
        Image = Image.Replace(System.Environment.NewLine, "");
        if (mysql)
        {
            string lang = uri.Contains("/ru/") ? "ru" : "az";
            saveScrapToMysql(uri, Category, Date, Image, lang, Body, Title);
            iiii++;
            return;
        }
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var finalPath = Path.Combine(basePath, SavePath);
        StreamWriter sw = new StreamWriter(finalPath, true);
        sw.WriteLine($"{uri.Trim()};{Category.Trim()};{Title.Trim()};{Date.Trim()};{Body.Trim()};{Image.Trim()};");
        sw.Flush();
        sw.Close();
        iiii++;
    }
    catch (Exception e)
    {
        _ = e;
    }
}
void fillVisitedFromData()
{
    foreach (var line in File.ReadLines($"{finalPathX}"))
    {
        try
        {
            var data = line.Split(';');
            visited.Add(data[0]);
            n++;
        }
        catch (Exception e)
        {
            _ = e;
        }
    }
    Console.WriteLine($"{visited.Count()} Pages Loaded to visited");
}
void fillLinksFromData()
{
    foreach (var line in File.ReadLines($"{finalPathL}"))
    {

        if (!line.Contains("news/read"))
        {
            continue;
        }
        try
        {
            if (queue.Contains(line)) continue;
            if (visited.Contains(line)) continue;
            queue.Enqueue(line);
        }
        catch (Exception e)
        {
            _ = e;
        }
    }
    Console.WriteLine($"{queue.Count()} Pages Loaded to queue");
}
void CheckDateinData(bool sql = false)
{
    ru = 0;
    az = 0;
    n = 0;
    if (sql)
    {
        MySqlConnection conn = new MySqlConnection(con);
        conn.Open();
        MySqlCommand comm = conn.CreateCommand();
        comm.CommandText = "select lang from new_news";
        MySqlDataReader msdr;
        msdr = comm.ExecuteReader();
        while (msdr.Read())
        {
            try
            {
                string lang = msdr.GetString(0);
                if (lang == "ru") ru++;
                else az++;
                n++;
            }
            catch (Exception e)
            {
                _ = e.Message;
            }
        }
        msdr.Close();
    }
    else
    {
        foreach (var line in File.ReadLines($"{finalPathX}"))
        {
            try
            {
                var data = line.Split(';');
                string uri = data[0];
                string lang = uri.Contains("/ru/") ? "ru" : "az";
                string date = data[3].Split("(")[0];
                bool pas = false;
                foreach (var d in translateDate)
                {
                    if (date.Contains(d.Key))
                    {
                        date = date.Replace(d.Key, d.Value);
                        pas = true;
                        break;
                    }
                }
                if (!pas) continue;
                DateTime dt = DateTime.Parse(date);
                if (dt <= minDate || dt >= maxDate) continue;

                if (lang == "ru") ru++;
                else az++;
                n++;
            }
            catch (Exception e)
            {
                _ = e;
            }
        }
    }
    Console.WriteLine($"lang =\n az:{az}, ru:{ru}");
    Console.WriteLine($"total={n}");
}
void loadDataFromMysql()
{
    MySqlConnection conn = new MySqlConnection(con);
    conn.Open();
    MySqlCommand comm = conn.CreateCommand();
    comm.CommandText = "SELECT * FROM `scrap_new_news`";
    var msdr = comm.ExecuteReader();

    while (msdr.Read())
    {
        hs.Add(msdr.GetString(0).Split("/").Last() ?? "");
    }
    msdr.Close();
    comm.ExecuteNonQuery();
    conn.Close();
}
void CrowlRange(int start = 483452, int end = 485591)
{
    n = 0;
    Dictionary<string, int> CountRetry = new();
    Queue<string> articlesList = new Queue<string>();
    List<string> list = new List<string>();

    MySqlConnection conn = new MySqlConnection(con);
    conn.Open();
    MySqlCommand comm = conn.CreateCommand();
    MySqlDataReader msdr;
    comm.CommandText = "SELECT uri FROM `scrap_new_news`";
    msdr = comm.ExecuteReader();
    if (msdr.Read())
    {
        list.Add(msdr.GetString(0)?.Split("/")?.Last() ?? "");
    }
    msdr.Close();
    for (int i = start; i <= end; i++)
    {
        if (list.Contains(i.ToString())) continue;
        articlesList.Enqueue(i.ToString());
    }
    while (articlesList.Count > 0)
    {
        n++;
        string art = articlesList.Dequeue();
        Queue<string> tmp = new();
        string uri = art;
        uri = $"http://web.archive.org/web/http://salamnews.org/ru/news/read/{uri}";
        if (hs.Contains(uri.Split("/").Last() ?? "__")) continue;
        HttpResponseMessage response;
        try
        {
            response = client.GetAsync(uri).Result;
        }
        catch (Exception e)
        {
            _ = e.Message;
            continue;
        }
        if (response is null) continue;
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(response.StatusCode);

            continue;
        }
        Stream stream = response.Content.ReadAsStreamAsync().Result;
        savePageData(stream, uri, "salamnews.range.data", true);
        Console.Clear();
        Console.WriteLine($"New: {hs.Count()}");
        Console.WriteLine($"Remain: {articlesList.Count}");
        Console.WriteLine($"Try: {n}");
        continue;
        if (!hs.Contains(art)) articlesList.Enqueue(art);
    }
}

confHandlers();
snapshots.Add("20101230050307");


//updateCate();
//updateDate();

//CrowlRange();


//loadDataFromMysql();
//fillVisitedFromData();
fillLinksFromData();
Crowl(snapshots.FirstOrDefault());


//dataToMysql();
//mysqlToMysql();

CheckDateinData(true);
Console.ReadKey();