using Leaf.xNet;
using MaterialDesignColors;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WebpWrapper;
using XLib.UserControls;

namespace P3_Login_Voz
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            InitializeHttpRequest();
        }

        private void InitializeHttpRequest()
        {
            httpRequest.Cookies = new CookieStorage();
            httpRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";
            httpRequest.CharacterSet = Encoding.UTF8;
            httpRequest.IgnoreProtocolErrors = true;
        }

        const string VOZ_BASEURL = "https://voz.vn/";
        const string USER_BASEURL = "https://voz.vn/u/";
        const string POST_BASEURL = "https://voz.vn/p/";
        const string TOPIC_BASEURL = "https://voz.vn/t/";

        HttpRequest httpRequest = new HttpRequest();
        List<VozPost> vozPosts = new List<VozPost>();
        HashSet<string> kissPosts = new HashSet<string>();
        HashSet<string> brickPosts = new HashSet<string>();
        string currentUser = "";
        string currentMemberId = "";

        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            currentUser = txbUsername.Text;
            Directory.CreateDirectory(currentUser);
            //Load cookie
            if (File.Exists(currentUser + "/cookies.bin"))
            {
                httpRequest.Cookies = CookieStorage.LoadFromFile(currentUser + "/cookies.bin");
                var html = httpRequest.Get("https://voz.vn/").ToString();
                File.WriteAllText("html_login.html", html);
                btnLogin.IsEnabled = false;
                pwbPassword.Clear();
                txblWaitForAWhite.Text = "Success!";

                Task.Run(() =>
                {
                    var (source, _username) = GetLoginAvatarSourceAsync(html).Result;
                    var bitmapImage = GetImageFromUrlAsync(source).Result;
                    Dispatcher.InvokeAsync(() =>
                    {
                        txblUsername.Text = _username;
                        imgUserAvatar.Source = bitmapImage;
                    });
                });
                               
                btnSearchPost.IsEnabled = true;
                //ProcessFile("html_login.html");
                return;
            }

            //Nếu không có cookie sẽ đăng nhập từ đầu             
            var password = pwbPassword.Password;
            ThreadPool.QueueUserWorkItem(async state => await LoginAsync(currentUser, password));
        }
        private async Task LoginAsync(string username, string password)
        {
            _ = Dispatcher.InvokeAsync(() =>
            {
                txblWaitForAWhite.Text = "Chờ một chút...";
                btnLogin.IsEnabled = false;
                pwbPassword.Clear();
            });

            var html = httpRequest.Get(VOZ_BASEURL).ToString();
            //lấy _xfToken đầu tiên để resquest login
            var _xfToken_resquest_login = Regex.Match(html, @"data-csrf=""(?<g1>.*?)""", RegexOptions.Singleline).Groups["g1"].Value;

            AddHeaderBeforeGetJson();

            //resquest này trả về mảng byte nén bằng brotli chứa _xfToken để login
            var brotliBytes = httpRequest.Get($"https://voz.vn/login/?_xfRequestUri=%2F&_xfWithData=1&_xfToken={WebUtility.UrlEncode(_xfToken_resquest_login)}&_xfResponseType=json").ToBytes();

            var jsonBytes = await BrotliDecompressAsync(brotliBytes);

            var json = Encoding.UTF8.GetString(jsonBytes);

            var _xfToken_login = Regex.Match(json, @"value=\\""(?<g1>.*?)\\""", RegexOptions.Singleline).Groups["g1"].Value;
            //xuất hiện login_two_step => login bằng password thành công và hiện tiếp xác nhận 2 step
            //xuất hiện forum_list => login thành công
            var html_login = httpRequest.Post($"https://voz.vn/login/login", $"_xfToken={WebUtility.UrlEncode(_xfToken_login)}&login={username}&password={password}&remember=1&_xfRedirect=https%3A%2F%2Fvoz.vn%2F", "application/x-www-form-urlencoded").ToString();

            var html_login_success = "";
            if (html_login.Contains("login_two_step"))
            {
                html_login_success = LoginTwoStep(html_login);
            }
            else
            {
                html_login_success = html_login;
            }

            httpRequest.Cookies.SaveToFile(username + "/cookies.bin");

            var (source, _username) = await GetLoginAvatarSourceAsync(html_login_success);
            var bitmapImage = await GetImageFromUrlAsync(source);

            _ = Dispatcher.InvokeAsync(() =>
            {
                txblWaitForAWhite.Text = "Success!";
                imgUserAvatar.Source = bitmapImage;
                txblUsername.Text = _username;
                btnSearchPost.IsEnabled = true;
            });

            File.WriteAllText("html_login.html", html_login_success);
            ProcessFile("html_login.html");
        }
        private async Task<byte[]> BrotliDecompressAsync(byte[] brotliBytes)
        {
            using var inputStream = new MemoryStream(brotliBytes);
            using var outputStream = new MemoryStream();
            using var compressionStream = new BrotliStream(inputStream, CompressionMode.Decompress);
            await compressionStream.CopyToAsync(outputStream);
            var bytes = outputStream.ToArray();
            return bytes;
        }
        private async Task<string> BrotliDecompressToStringAsync(byte[] brotliBytes)
        {
            var bytes = await BrotliDecompressAsync(brotliBytes);
            return Encoding.UTF8.GetString(bytes);
        }
        private void AddHeaderBeforeGetJson()
        {
            httpRequest.AddHeader("Accept", "application/json, text/javascript, */*; q=0.01");
            httpRequest.AddHeader("Accept-Encoding", "gzip, deflate, br");
            httpRequest.AddHeader("Accept-Language", "en-US,en;q=0.9");
            httpRequest.AddHeader("Referer", "https://voz.vn/");
            httpRequest.AddHeader("Sec-Ch-Ua", "\"Not.A/Brand\";v=\"8\", \"Chromium\";v=\"114\", \"Google Chrome\";v=\"114\"");
            httpRequest.AddHeader("Sec-Ch-Ua-Mobile", "?0");
            httpRequest.AddHeader("Sec-Ch-Ua-Platform", "\"Windows\"");
            httpRequest.AddHeader("Sec-Fetch-Dest", "empty");
            httpRequest.AddHeader("Sec-Fetch-Mode", "cors");
            httpRequest.AddHeader("Sec-Fetch-Site", "same-origin");
            httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
        }
        private async Task<(string Source, string Username)> GetLoginAvatarSourceAsync(string html)
        {
            var _xfToken = Regex.Match(html, @"data-csrf=""(?<g1>.*?)""", RegexOptions.Singleline).Groups["g1"].Value;

            AddHeaderBeforeGetJson();

            var brotliBytes = httpRequest.Get($"https://voz.vn/account/visitor-menu?_xfRequestUri=%2F&_xfWithData=1&_xfToken={WebUtility.UrlEncode(_xfToken)}&_xfResponseType=json").ToBytes();
            var jsonBytes = await BrotliDecompressAsync(brotliBytes);
            var json = Encoding.UTF8.GetString(jsonBytes);
            var match = Regex.Match(json, @"srcset=\\""(?<g1>[\w\/\.\:? ]*?)\\"" alt=\\""(?<g2>.*?)\\""", RegexOptions.Singleline);
            var source = match.Groups["g1"].Value.Replace(" ", "");
            var username = Regex.Unescape(match.Groups["g2"].Value);
            return (source, username);
        } 
        private async Task<(string Source, string MemberName)> GetMemberAvatarSourceAsync(string userId)
        {
            var html = await Task.Run(() => httpRequest.Get($"{USER_BASEURL}{userId}/").ToString());

            var matches = Regex.Matches(html, @"src=""(?<g1>https://statics\.voz\.tech[\w\/\.\:? ]*?)""(.*?)alt=""(?<g2>.*?)""", RegexOptions.Singleline);
            if(matches.Count > 2)
            {
                var source = matches[2].Groups["g1"].Value.Replace(" ", "");
                var memberName = Regex.Unescape(matches[2].Groups["g2"].Value);
                return (source, memberName);
            }
            return ("https://statics.voz.tech/styles/next/xenforo/smilies/popopo/sad.png", "[profile limited]");

        }
        private string LoginTwoStep(string html_two_step)
        {
            //lấy _xfToken để xác nhận 2 step
            var _xfToken_two_step = Regex.Match(html_two_step, @"data-csrf=""(?<g1>.*?)""", RegexOptions.Singleline).Groups["g1"].Value;

            //Chờ code từ email
            var code = Dispatcher.Invoke(() => EnterCode.Show());

            var html_login_success = httpRequest.Post($"https://voz.vn/login/two-step", $"_xfToken={WebUtility.UrlEncode(_xfToken_two_step)}&code={code}&trust=1&confirm=1&provider=email&remember=1&_xfRedirect=https%3A%2F%2Fvoz.vn%2F", "application/x-www-form-urlencoded").ToString();

            return html_login_success;
        }
        private void ProcessFile(string filename)
        {
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo() { UseShellExecute = true, FileName = filename }
            };
            process.Start();
        }
        private void ButtonSearchPost_Click(object sender, RoutedEventArgs e)
        {
            ckbContinueSearch.IsChecked = true;
            currentMemberId = txbUserId.Text;
            if (string.IsNullOrWhiteSpace(currentMemberId)) return;
            ThreadPool.QueueUserWorkItem(state => SearchPostByUserId(currentMemberId));
        }
        private async void SearchPostByUserId(string userId)
        {
            var (source, memberName) = await GetMemberAvatarSourceAsync(userId);
            var bitmapImage = await GetImageFromUrlAsync(source);

            _ = Dispatcher.InvokeAsync(() =>
            {
                txblWaitForAWhite.Text = "Chờ một chút...";
                btnSearchPost.IsEnabled = false;
                btnStartReact.IsEnabled = false;
                imgMemberAvatar.Source = bitmapImage;
                Dispatcher.Invoke(priority: System.Windows.Threading.DispatcherPriority.Background, () => { });
                txblMemberName.Text = memberName;
            });

            LoadReactionLog(userId);
            _ = Dispatcher.InvokeAsync(() =>
            {
                var isBrick = btnIsBrick.IsChecked is false ? false : true;
                if (isBrick) runTotalReactions.Text = brickPosts.Count.ToString();
                else runTotalReactions.Text = kissPosts.Count.ToString();
            });

            var postPath = "Posts/" + userId + "_posts.txt";
            if (File.Exists(postPath))
            {
                LoadSearchLog(userId);
                var filePath = "Posts/" + userId + "_last.txt";
                if (File.Exists(filePath))
                {
                    var pageUrl = File.ReadAllText(filePath);
                    File.Delete(filePath);
                    var fromPage = 1;
                    if (pageUrl.Contains("?page="))
                    {
                        var page = Regex.Match(pageUrl, @"\?page=(?<g1>\d+)&", RegexOptions.Singleline).Groups["g1"].Value;
                        if (int.TryParse(page, out int result)) fromPage = result;

                        pageUrl = Regex.Replace(pageUrl, @"page=(\d+)&", "", RegexOptions.Singleline);
                    }
                    StartSearch(pageUrl, userId, fromPage);

                    SaveSearchLog(userId);
                }
            }
            else
            {
                var html = httpRequest.Get($"https://voz.vn/search/member?user_id={userId}").ToString();

                if (!html.Contains("No results found."))
                {
                    vozPosts.Clear();

                    var searchUrl = Regex.Match(html, @"og:url"" content=""(?<g1>.*?)""", RegexOptions.Singleline).Groups["g1"].Value;
                    StartSearch(searchUrl, userId);

                    SaveSearchLog(userId);
                }
            }
           

            _ = Dispatcher.InvokeAsync(() =>
            {
                txblWaitForAWhite.Text = "Success!";
                runTotalPosts.Text = vozPosts.Count.ToString();
                btnSearchPost.IsEnabled = true;
                btnStartReact.IsEnabled = true;
            });

        }
        private void StartSearch(string urlWithoutPage, string userId, int fromPage = 1)
        {
            //https://voz.vn/search/9066232/?c[users]=Le_Rachitique&o=date
            //https://voz.vn/search/9066036/?c[older_than]=1678784631&c[users]=Le_Rachitique&o=date
            //tách ra url search đầu tiên để tạo url search theo từng page
            var temp = WebUtility.HtmlDecode(urlWithoutPage).Split("?");
            var url_left = temp[0] + "?page=";
            var url_right = "&" + temp[1];

            var page = fromPage;
            while (true)
            {
                var url_page = url_left + page.ToString() + url_right;
                var html_page = httpRequest.Get(url_page).ToString();
                //lọc ra 20 post mỗi html page
                var matches = Regex.Matches(html_page, @"""contentRow-title"">(\s*?)<a href=""(?<g1>.*?)""", RegexOptions.Singleline);
                foreach (Match match in matches)
                {
                    if (continueSearch is false)
                    {
                        File.WriteAllText("Posts/" + userId + "_last.txt", url_page);
                        return;
                    }

                    var m = Regex.Match(match.Groups["g1"].Value, @"^(?<g1>\/t\/(.*?)\/)post-(?<g2>\d+?)$", RegexOptions.Singleline);
                    var topicUrl = m.Groups["g1"].Value;
                    var id = m.Groups["g2"].Value;
                    //nếu post này là 1 topic (post#1) thì sẽ truy cập topic để lấy tiếp post id
                    if (string.IsNullOrWhiteSpace(topicUrl) && string.IsNullOrWhiteSpace(id))
                    {
                        topicUrl = match.Groups["g1"].Value;
                        var html_topic = httpRequest.Get(VOZ_BASEURL + topicUrl).ToString();
                        id = Regex.Match(html_topic, @"class=""message message--post js-post js-inlineModContainer(.*?)""(.*?)data-content=""post-(?<g1>\d+?)""", RegexOptions.Singleline).Groups["g1"].Value;
                        //Nếu vẫn không tìm được thì bỏ luôn
                        if (string.IsNullOrWhiteSpace(id))
                        {
                            //Debugger.Break();

                            continue;
                        }
                    }

                    vozPosts.Add(new VozPost { TopicUrl = topicUrl, Id = id, UserId = userId });
                    _ = Dispatcher.InvokeAsync(() => runTotalPosts.Text = vozPosts.Count.ToString());
                }
                //do số post <= 20 sẽ không hiện nút [View older results] khiến vòng lặp mãi không dừng
                //ta sẽ thoát search nếu member dưới 20 post nhưng member có đúng 20 post sẽ lỗi lặp vô hạn
                if (matches.Count < 20) return;
                //quá 10 page hoặc đến page cuối sẽ gặp nút [View older results]
                if (html_page.Contains("block-footer-controls"))
                {
                    //lấy url search 10 page tiếp theo trong nút [View older results]
                    // /search/9066232/older?before=1678784631&amp;c[users]=Le_Rachitique&amp;o=date
                    var viewOlderResultsUrl = Regex.Match(html_page, @"class=""block-footer-controls"">(\s*?)<a href=""(?<g1>.*?)""", RegexOptions.Singleline).Groups["g1"].Value;
                    var html_older = httpRequest.Get(VOZ_BASEURL + viewOlderResultsUrl).ToString();
                    if (html_older.Contains("No results found.")) break;

                    //https://voz.vn/search/9066036/?c[older_than]=1678784631&c[users]=Le_Rachitique&o=date
                    var searchOlderUrl = Regex.Match(html_older, @"og:url"" content=""(?<g1>.*?)""", RegexOptions.Singleline).Groups["g1"].Value;
                    try
                    {
                        StartSearch(WebUtility.HtmlDecode(searchOlderUrl), userId);
                    }
                    catch(Exception ex)
                    {
                        Dispatcher.InvokeAsync(() => DialogBox.Show(searchOlderUrl + "\n" + ex.Message, "AutoKissVoz"));
                        File.WriteAllText("Posts/" + userId + "_last.txt", url_page);
                        SaveSearchLog(userId);
                    }

                    break;
                }
                page++;
            }
        }
        private async Task<BitmapImage> GetImageFromUrlAsync(string imageUrl)
        {
            var bytes = await Task.Run(() => httpRequest.Get(imageUrl).ToBytes());
            MemoryStream memoryStream = new MemoryStream();

            try
            {
                using Webp webp = new Webp();
                var bitmap = webp.Decode(bytes);
                bitmap.Save(memoryStream, ImageFormat.Png);

            }
            catch
            {
                memoryStream.Close();
                memoryStream = new MemoryStream(bytes);
            }

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); //freeze để có thể pass sang các thread khác
            memoryStream.Close();
            return bitmapImage;

        }
        private void ButtonStartReact_Click(object sender, RoutedEventArgs e)
        {
            var isBrick = btnIsBrick.IsChecked is false ? false : true;
            if (vozPosts.Count < 1) return;
            ThreadPool.QueueUserWorkItem(state => StartReact(isBrick));
        }
        private void StartReact(bool isBrick = false)
        {
            _ = Dispatcher.InvokeAsync(() =>
            {
                txblWaitForAWhite.Text = "Chờ một chút...";
                btnSearchPost.IsEnabled = false;
                btnStartReact.IsEnabled = false;
            });

            var count = 0;
            foreach (var post in vozPosts)
            {
                Thread.Sleep(delay);

                if (isBrick && brickPosts.Contains(post.Id)) continue;
                if (!isBrick && kissPosts.Contains(post.Id)) continue;

                try
                {
                    React(post.Id, isBrick, out bool isMaximum);
                    if (isMaximum) break;
                }
                catch(Exception ex)
                {
                    Dispatcher.InvokeAsync(() => DialogBox.Show(POST_BASEURL + post.Id + "\n" + ex.Message, "AutoKissVoz"));
                    
                    continue;
                }
                
                _ = Dispatcher.InvokeAsync(() =>
                {
                    if (isBrick) runTotalReactions.Text = brickPosts.Count.ToString();
                    else runTotalReactions.Text = kissPosts.Count.ToString();
                });

                SaveReactionLog(post.UserId);
                count++;
            }

            var successMessage = $"{count} posts in Posts/{vozPosts[0].UserId}_posts.txt reacted." +
                $"\nReactions log: {currentUser}/{(isBrick ? "Bricks" : "Kisses")}/{vozPosts[0].UserId}_{(isBrick ? "bricks" : "kisses")}.txt." +
                $"\nRemove {vozPosts[0].UserId}_posts.txt to update and react lastest posts of this member.";

            _ = Dispatcher.InvokeAsync(() =>
            {
                txblWaitForAWhite.Text = "Success!";
                btnSearchPost.IsEnabled = true;
                btnStartReact.IsEnabled = true;
                DialogBox.Show(successMessage, "AutoKissVoz");
            });


        }
        private void ButtonTestReact_Click(object sender, RoutedEventArgs e)
        {
            var isBrick = btnIsBrick.IsChecked is false ? false : true;
            ThreadPool.QueueUserWorkItem(state => React("26539822", isBrick, out bool isMaximum));
        }
        private void React(string postId, bool isBrick, out bool isMaximum)
        {
            var url = POST_BASEURL + postId;
            var html = httpRequest.Get(url).ToString();
            //trong danh sách post chỉ tìm được post id và url topic nhưng để POST react được cần chỉ rõ cả page-xxx trong data
            var topicUrl = Regex.Match(html, @"rel=""canonical"" href=""https://voz\.vn(?<g1>.*?)""", RegexOptions.Singleline).Groups["g1"].Value;
            var _xfToken = Regex.Match(html, @"_xfToken"" value=""(?<g1>.*?)""", RegexOptions.Singleline).Groups["g1"].Value;

            var reaction_id = "1";
            if (isBrick) reaction_id = "2";
            var reaction_url = $"{url}/react?reaction_id={reaction_id}";
            var data = $"_xfRequestUri={WebUtility.UrlEncode(topicUrl)}&_xfWithData=1&_xfToken={WebUtility.UrlEncode(_xfToken)}&_xfResponseType=json";
            AddHeaderBeforeGetJson();

            var brotliBytes = httpRequest.Post(reaction_url, data, "application/x-www-form-urlencoded; charset=UTF-8").ToBytes();
            var json = BrotliDecompressToStringAsync(brotliBytes).Result;
            var response_reaction = JsonConvert.DeserializeObject<response_reaction>(json);

            /*
                * brick
                * linkReactionId: 2
                * reactionId: 2
                * 
                * kiss
                * linkReactionId: 1
                * reactionId: 1
                * 
                * remove reaction
                * linkReactionId: 1
                * reactionId: null
                */

            if (response_reaction != null && response_reaction.linkReactionId == 1 && response_reaction.reactionId == null)
            {
                brotliBytes = httpRequest.Post(reaction_url, data, "application/x-www-form-urlencoded; charset=UTF-8").ToBytes();
                json = BrotliDecompressToStringAsync(brotliBytes).Result;
                response_reaction = JsonConvert.DeserializeObject<response_reaction>(json);
            }
            else if(response_reaction != null && response_reaction.linkReactionId == 0)
            {
                Dispatcher.InvokeAsync(() => DialogBox.Show(json, "AutoKissVoz"));
                isMaximum = true;
                return;
            }

            isMaximum = false;
            if (isBrick && response_reaction != null && response_reaction.linkReactionId == 2 && response_reaction.reactionId.ToString() is "2") brickPosts.Add(postId);
            else if(!isBrick && response_reaction != null && response_reaction.linkReactionId == 1 && response_reaction.reactionId.ToString() is "1") kissPosts.Add(postId);
        }
        private void LoadSearchLog(string userId)
        {
            var postPath = "Posts/" + userId + "_posts.txt";
            var json_post = File.ReadAllText(postPath);
            var posts = JsonConvert.DeserializeObject<List<VozPost>>(json_post);
            if (posts != null) vozPosts = posts;
        }
        private void SaveSearchLog(string userId)
        {
            var json = JsonConvert.SerializeObject(vozPosts, Formatting.Indented);
            Directory.CreateDirectory("Posts");
            var postPath = "Posts/" + userId + "_posts.txt";
            File.WriteAllText(postPath, json);
            Dispatcher.InvokeAsync(() => DialogBox.Show($"{vozPosts.Count} posts saved in {postPath}.", "AutoKissVoz"));

        }
        private void LoadReactionLog(string userId)
        {
            var kissPath = currentUser + "/Kisses/" + userId + "_kisses.txt";
            kissPosts.Clear();
            if (File.Exists(kissPath))
            {
                var json_kiss = File.ReadAllText(kissPath);
                var kisses = JsonConvert.DeserializeObject<HashSet<string>>(json_kiss);
                if (kisses != null) kissPosts = kisses;
            }

            var brickPath = currentUser + "/Bricks/" + userId + "_bricks.txt";
            brickPosts.Clear();
            if (File.Exists(brickPath))
            {
                var json_brick = File.ReadAllText(brickPath);
                var bricks = JsonConvert.DeserializeObject<HashSet<string>>(json_brick);
                if (bricks != null) brickPosts = bricks;
            }
        }
        private void SaveReactionLog(string userId)
        {
            var json_kiss = JsonConvert.SerializeObject(kissPosts);
            Directory.CreateDirectory(currentUser + "/Kisses");
            File.WriteAllText(currentUser + "/Kisses/" + userId + "_kisses.txt", json_kiss);

            var json_brick = JsonConvert.SerializeObject(brickPosts);
            Directory.CreateDirectory(currentUser + "/Bricks");
            File.WriteAllText(currentUser + "/Bricks/" + userId + "_bricks.txt", json_brick);

        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (vozPosts.Count > 0)
            {
                var userId = vozPosts[0].UserId;

                SaveReactionLog(userId);

            }
        }
        private void btnIsBrick_Checked(object sender, RoutedEventArgs e)
        {
            runTotalReactions.Text = brickPosts.Count.ToString();

        }
        private void btnIsBrick_Unchecked(object sender, RoutedEventArgs e)
        {
            runTotalReactions.Text = kissPosts.Count.ToString();
        }
        bool continueSearch;
        private void ckbContinueSearch_Checked(object sender, RoutedEventArgs e)
        {
            continueSearch = true;
        }
        private void ckbContinueSearch_Unchecked(object sender, RoutedEventArgs e)
        {
            continueSearch = false;
        }
        int delay;
        private void nmDelay_ValueChanged(object sender, ValueChangedEventAgrs e)
        {
            delay = (int)nmDelay.Value;
        }
    }

    public class VozPost
    {
        public string TopicUrl { get; set; }
        public string Id { get; set; }
        public string UserId { get; set; }
    }
    public class response_request_login
    {
        public string status { get; set; }
        public response_request_login_html html { get; set; }
    }
    public class response_request_login_html
    {
        public string content { get; set; }
        public string title { get; set; }
        public string[] js { get; set; }
    }
    public class response_reaction
    {
        public response_reaction_html html { get; set; }
        public response_reaction_reactionlist reactionList { get; set; }
        public object reactionId { get; set; }
        public int linkReactionId { get; set; }
        public response_reaction_visitor visitor { get; set; }
    }
    public class response_reaction_html
    {
        public string content { get; set; }
    }
    public class response_reaction_reactionlist
    {
        public string content { get; set; }
    }
    public class response_reaction_visitor
    {
        public string conversations_unread { get; set; }
        public string alerts_unviewed { get; set; }
        public string total_unread { get; set; }
    }

}
