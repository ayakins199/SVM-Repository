using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace PDS_SVM
{
    public class PDSClass
    {
        public string extractedFeaturesFilePathTrainDS = String.Format(Environment.CurrentDirectory + "\\{0}", "ExtractedFeaturesTrain.txt");
        public string filenameTrainDS = "ExtractedFeaturesTrain.txt";
        public string extractedFeaturesFilePathTestDS = String.Format(Environment.CurrentDirectory + "\\{0}", "ExtractedFeaturesTest.txt");
        public string filenameTestDS = "ExtractedFeaturesTest.txt";
        

        public List<string> extractedFeatures = new List<string>();
        /// <summary>
        /// This method contains all the features used for classification
        /// </summary>
        public string[] Features()
        {
            //'http' checks for IP-Based domain names, 'href=' checks for non-matching urls(e.g <a href="badsite.com"> paypal.com</a>.)
            //<a> check the url text of all the urls (that links to the modal domain, i.e, the domain most frequently linked to) 
            //of each mail to confirm whether they contain any these words: "link","click","here"
            //'content-type:' checks for HTML emails if it contains a section that is denoted with a MIME type of text/html.
            //'href' count the number of dots in each domain names that is linked to. 
            //'javascript' checks the presence the string, "javascript" in each email
            //</a> counts the number of links in an email, linkedToDomain counts the number of distinct domains that is linked to in each email
            //From_Body_MatchDomain check domain name mismatch between sender's(FROM:) field(e.g sender@paypal.com) and the email's body
            //'<form' is a feature that checks for the presence of <form> tag in the email
            //'update' checks the email for the occurence of 'update' and 'confirm', 'user' checks the email for 'user', 'customer' & 'client'
            //'suspend' checks for 'suspend', 'restrict' & 'hold', 'verify' checks for 'verify' & 'account'
            //'login' checks for 'login', 'username' & 'password', 'ssn' checks for 'ssn' and 'social security'
            //string[] featrs = new string[] { "http", "href=", "<a>", "content-type:", "href", "javascript", "</a>","linkedToDomain",
            //                                 "From_Body_MatchDomain", "update","user", "suspend", "verify", "login", "ssn", "spamassassin","bankNames"};
            string[] featrs = new string[] { "http", "href=", "<a>", "content-type:", "href", "javascript", "</a>","linkedToDomain",
                                             "From_Body_MatchDomain", "update","user", "suspend", "verify", "login", "ssn", "SpamAssassin", "bankNames"};

            return featrs;
        }

        /// <summary>
        /// Process each emails for vector value extraction
        /// </summary>
        public void processVector(int[,] vector, Dictionary<string, int> mails, string[] features, string[] mailURLs, int NumOfFeatures)
        {
            PDSClass pds = new PDSClass();
            string[] URLFeatures = { "login", "update", "click", "here" };
            //assign vector to phishing emails
            int i; int j;
            for (i = 0; i < mails.Count; i++)
            {
                List<string> mail = mails.ElementAt(i).Key.Split(' ').ToList();
                for (j = 0; j < NumOfFeatures; j++)
                {
                    //checking the phishing emails for each features
                    vector[i, j] = 0; //asigning zero to vector[] if the currently checked feature dosen't exist in the current mail
                    pds.AssignVector(vector, mails, mailURLs[i], features[j], mail, i, j); //start assigning vector to each phish mail
                }
            }

        }

        /// <summary>
        /// Extract the vector values of each email
        /// </summary>
        public void AssignVector(int[,] vector, Dictionary<string, int> mails, string url, string Feature, List<string> mail, int i, int j)
        {
            StreamReader reader = new StreamReader(url);
            string htmlContent = reader.ReadToEnd();
            int ctr = 0; string join;
            htmlContent = htmlContent.ToLower(); //convert email to lower case for easy matching

            //string[] fetures = Features();
            string[] fetures = Features();
            if (fetures.Contains(Feature))
            {

                if (Feature.Equals("content-type:") || Feature.Equals("verify"))
                {
                    ctr = 0;
                    foreach (string m in mail)
                    {
                        if (m.Equals("content-type:") && Feature.Equals("content-type:"))
                        {
                            join = mail[ctr] + " " + mail[++ctr];
                            vector[i, j] = (join.Equals("content-type: text/html") || join.Equals("content-type: multipart/alternative")) ? 1 : 0;
                            if (vector[i, j] == 1) break;
                        }
                        else if (Feature.Equals("verify") && m.Equals("verify") && (join = string.Format("{0} {1} {2}", mail[ctr], mail[++ctr], mail[++ctr])) == "verify your account")
                        {
                            vector[i, j] = 1;
                            break;
                        }
                        ctr++;
                    }

                }
                else if (Feature.Equals("href"))
                {
                    vector[i, j] = DomainNameDotCount(htmlContent); //assigning the number of dots in the domain of each email
                }
                else if (Feature.Equals("href="))
                {
                    if (urlTarDifLinks(htmlContent))
                    {
                        vector[i, j] = 1;
                        // break;
                    }
                }
                else if (Feature.Equals("http"))
                {
                    if (urlDetection(htmlContent))
                    {
                        vector[i, j] = 1;
                        //break;
                    }
                }
                else if (Feature.Equals("<img"))
                {
                    if (imgLinkDetection(htmlContent))
                    {
                        vector[i, j] = 1;
                        //break;
                    }
                }
                else if (Feature.Equals("<a>"))
                {
                    int numOfDomain; //note, this variable is a dummy variable for dis feature. it is only useful when Feature == "linkedToDomain"
                    if (getURLText(htmlContent, vector, Feature, i, j, out numOfDomain))
                        vector[i, j] = 1;
                    //break;
                }
                else if (Feature.Equals("javascript"))
                {
                    if (containsJavaScript(htmlContent))
                        vector[i, j] = 1;
                    //break;
                }
                else if (Feature.Equals("</a>"))
                {
                    vector[i, j] = countNumberOfLinks(htmlContent); //assigning the total number of links in each emails
                    //break;
                }
                else if (Feature.Equals("linkedToDomain"))
                {
                    int numOfDomain;
                    getURLText(htmlContent, vector, Feature, i, j, out numOfDomain);
                    vector[i, j] = numOfDomain;//this variable is used to get the total number of domains in the urls of each email
                    //break; 
                }
                else if (Feature.Equals("From_Body_MatchDomain"))
                {
                    if (checkFrom_Body_DomainNameMismatch(url, htmlContent))
                        vector[i, j] = 1;
                }
                //processing the Word Features
                else if (Feature.Equals("update"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent);
                }
                else if (Feature.Equals("user"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent);
                }
                else if (Feature.Equals("suspend"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent);
                }
                else if (Feature.Equals("verify"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent);
                }
                else if (Feature.Equals("login"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent);
                }
                else if (Feature.Equals("ssn"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent); 
                }
                else if (Feature.Equals("bankNames"))
                {
                    vector[i, j] = checkKeyWords(url, Feature, htmlContent);
                }
                else if (Feature.Equals("SpamAssassin"))
                {
                    vector[i, j] = spamassassin(url, mails, i);
                }
                else
                {
                    if (htmlContent.Contains(Feature))
                    {
                        vector[i, j] = 1;
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the dataset entropy
        /// </summary>
        public double Entropy(Dictionary<string, int> mails)
        {
            int TotalMails; Double DivH = 0.0, DivP, DatasetEntropy = 0.0;
            int TotalPEmail = 0, TotalHEmail = 0;

            foreach (KeyValuePair<string, int> val in mails)
            {
                if (val.Value.Equals(1))
                    TotalPEmail++;
                else
                    TotalHEmail++;
            }

            TotalMails = TotalPEmail + TotalHEmail;
            DivH = (double)TotalHEmail / (double)TotalMails;
            DivP = (double)TotalPEmail / (double)TotalMails;
            DatasetEntropy = -(DivP * Math.Log(DivP, 2)) + -(DivH * Math.Log(DivH, 2));

            return DatasetEntropy;
        }

        /// <summary>
        /// Calculating the information gain
        /// </summary>
        public void CalInformationGain(int FNum, int[,] HPCount, double[] informationGain, int TMails, double DSEntropy)
        {
            int l = 0;
            for (int i = 0; i < FNum; i++)
            {
                double DivHam, DivPhish, hamEntropy, phishEntropy;
                for (int j = 0; j < 1; j++)
                {
                    //calculating the entropy for each feature
                    DivHam = (HPCount[i, j] + HPCount[i, j + 1]) / (double)TMails;
                    hamEntropy = CalculateEntropy(HPCount[i, j + 1], HPCount[i, j]);//calculating the ham entropy for this feature
                    DivPhish = (HPCount[i, j + 2] + HPCount[i, j + 3]) / (double)TMails;
                    phishEntropy = CalculateEntropy(HPCount[i, j + 3], HPCount[i, j + 2]);//calculating the phishing entropy for this feature

                    informationGain[l++] = DSEntropy - (DivHam * hamEntropy) - (DivPhish * phishEntropy);
                }
            }
        }

        /// <summary>
        /// Calculating the information gain for credit card dataset
        /// </summary>
        public void CalInformationGainCreditCard(int FNum, double[,] HPCount, double[] informationGain, int TMails, double DSEntropy)
        {
            int l = 0;
            for (int i = 0; i < FNum; i++)
            {
                double DivHam, DivPhish, hamEntropy, phishEntropy;
                for (int j = 0; j < 1; j++)
                {
                    //calculating the entropy for each feature
                    DivHam = (HPCount[i, j] + HPCount[i, j + 1]) / (double)TMails;
                    hamEntropy = CalculateEntropyCreditCard(HPCount[i, j + 1], HPCount[i, j]);//calculating the ham entropy for this feature
                    DivPhish = (HPCount[i, j + 2] + HPCount[i, j + 3]) / (double)TMails;
                    phishEntropy = CalculateEntropyCreditCard(HPCount[i, j + 3], HPCount[i, j + 2]);//calculating the phishing entropy for this feature

                    informationGain[l++] = DSEntropy - (DivHam * hamEntropy) - (DivPhish * phishEntropy);
                }
            }
        }

        /// <summary>
        /// Calculate Entropy
        /// </summary>
        public double CalculateEntropyCreditCard(double TotalPEmail, double TotalHEmail)
        {
            double TotalMails; Double DivH, DivP, Entropy;
            TotalMails = TotalPEmail + TotalHEmail;
            DivH = (double)TotalHEmail / (double)TotalMails;
            DivP = (double)TotalPEmail / (double)TotalMails;
            Entropy = -(DivH * Math.Log(DivH, 2)) + -(DivP * Math.Log(DivP, 2));
            Entropy = Entropy.Equals(double.NaN) ? 0.0 : Entropy;
            return Entropy;
        }

        /// <summary>
        /// Calculate Entropy
        /// </summary>
        public double CalculateEntropy(int TotalPEmail, int TotalHEmail)
        {
            int TotalMails; Double DivH, DivP, Entropy;
            TotalMails = TotalPEmail + TotalHEmail;
            DivH = (double)TotalHEmail / (double)TotalMails;
            DivP = (double)TotalPEmail / (double)TotalMails;
            Entropy = -(DivH * Math.Log(DivH, 2)) + -(DivP * Math.Log(DivP, 2));
            Entropy = Entropy.Equals(double.NaN) ? 0.0 : Entropy;
            return Entropy;
        }

        /// <summary>
        /// Calculating the total number of domain names in a url
        /// </summary>
        public int DomainNameDotCount(string Email)
        {
            //bool retVal = false;
            string[] domSplit = { }; int dotCount = 0;
            int Count = CountWordOccurences(Email, "href"); int m = 0;
            //getting the domain name in this href (i.e in the url)
            while (Count > 0)
            {
                int idx = Email.IndexOf("href");
                if (idx != -1)
                {
                    idx = Email.IndexOf("href", ++m);
                    if (idx != -1)
                    {
                        int idx2 = Email.IndexOf(">", idx);
                        if (idx2 != -1)
                        {
                            string url = Email.Substring(idx, idx2 - idx);

                            int idx3 = url.IndexOf("http");
                            if (idx3 != -1)
                            {
                                int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
                                if (idx4 != -1)
                                {
                                    //string domainName = url.Substring(idx3, idx4 - idx3); // get the domain name
                                    string domainName = getDomainNameFromURL(url); // get the domain name
                                    domSplit = domainName.Split('.'); //split the domain name to count the number of dots in the url
                                    dotCount += domSplit.Length - 1;
                                   
                                }
                            }
                        }
                    }
                }
                m = idx;
                Count--;
            }
            return dotCount;
        }

        /// <summary>
        /// This method gets the index of the Nth occurence of a character in the string
        /// </summary>
        public int IndexOfNth(string str, char c, int n, int StartIndex)
        {
            int s = 0;

            for (int i = 0; i < n; i++)
            {
                s = str.IndexOf(c, StartIndex + 1);

                if (s == -1) break;

                StartIndex = s;
            }

            return s;
        }

        /// <summary>
        /// Get domain name from url
        /// </summary>
        public string getDomainNameFromURL(string url)
        {
            int idDomain2 = -1, idDomain1;
            string urlDomain = ""; string domainName = ""; Uri uri;
            url = url.Trim(); int index; int idx2 = 0;
            idDomain1 = url.IndexOf("http"); //get the index of the string "http" in the url
            if (idDomain1 != -1) //if the string exist then proceed (if the string does not exist then index will return "-1")
                idDomain2 = url.IndexOf(">", idDomain1);
            if (idDomain2 != -1)
            {
                urlDomain = url.Substring(idDomain1, idDomain2 - idDomain1 + 1); //get the substring within the specified index
                index = (urlDomain.IndexOf("\">") == -1) ? urlDomain.IndexOf(">") : urlDomain.IndexOf("\">"); //get the index of "">" or ">" in url 
                urlDomain = urlDomain.Remove(index);//remove all the other characters starting from indexOf(">") bcos they are useless(or they are not part of the domain)

                if (Uri.TryCreate(urlDomain, UriKind.Absolute, out uri))
                {
                    uri = new Uri(urlDomain);
                    domainName = uri.Host; //get the domain name from the url
                }
                else //if the url cannot be parsed by URi, then strip domain name from url manually
                {
                    int idx1 = url.IndexOf("http");
                    if (idx1 != -1)
                    {
                        idx2 = (url.IndexOf("\"", idx1) == -1) ? url.IndexOf("<", idx1) : url.IndexOf("\"", idx1); //get the domain name either within a tag(<a href="www.yah.com") or in between a tag(<font>www.yah.com</font>)
                        domainName = url.Substring(idx1, idx2 - idx1).Split(new string[] { "//" }, StringSplitOptions.None).Last();
                    }

                }
            }

            return domainName;
        }

        /// <summary>
        /// Processing the spam assassin feature - i.e. assigning the classification result of SpamAssassin to each email
        /// Note that, each emails has already been classified by spamAssassin. We are only assigning the classification result here
        /// </summary>
        public int spamassassin(string mailURLs, Dictionary<string, int> mails, int i)
        {
            string[] split = mailURLs.Split('\\');
            int retVal = mails.ElementAt(i).Value;
      
            return retVal;
        }

        /// <summary>
        /// This method checks emails for the presence of URLs whose target domain name is different from the domain name displayed in the url text
        /// </summary>
        public bool urlTarDifLinks(string Email)
        {
            bool retVal = false;
            string domainName2 = "";
            int Count = CountWordOccurences(Email, "href=");

            //getting the domain name in this href (i.e in the url)
            int idx = Email.IndexOf("href="); int m = 0;
            while (Count > 0)
            {
                if (idx != -1)
                {
                    idx = Email.IndexOf("href=", ++m);
                    int idx2 = Email.IndexOf("</a>", idx);
                    if (idx2 != -1)
                    {
                        string url = Email.Substring(idx, idx2 - idx + 4);

                        int idx3 = url.IndexOf("http");
                        int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
                        if (idx4 != -1)
                        {
                            string domainName1 = getDomainNameFromURL(url); // get the domain name
                            int idx5 = url.IndexOf(">"); int idx6 = url.IndexOf("</a>");
                            string reslt = url.Substring(idx5, idx6 - idx5);
                            int idx7 = reslt.IndexOf("http");
                            int idx8 = IndexOfNth(reslt, '/', 3, idx7);
                            if (idx8 != -1)
                            {
                                string domainName = getDomainNameFromURL(reslt); // get the domain name

                                if (!domainName1.Equals(domainName2) && domainName2.Split('.').Length == 3) //comparing the two domain names in the present url
                                {
                                    retVal = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                Count--;
                m = idx;
            }

            return retVal;
        }

        /// <summary>
        /// using regualar expression to count the occurence of certain words in an email
        /// </summary>
        public int CountWordOccurences(string Email, string pattern)
        {
            int TotalOccurence = Regex.Matches(Email, pattern).Count;

            return TotalOccurence;
        }

       /***
        public bool checkFileForDuplicate(string feature)
        {
            bool retVal = false;
            string inputString = File.ReadAllText(filenameTrainDS);
            string[] split = inputString.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            if (split.Contains(feature))
                retVal = true;

            return retVal;
        }
        ****/

        /// <summary>
        /// This method checks each email to see whether the domain name in each urls contain IP address
        /// </summary>
        public bool urlDetection(string Email)
        {
            bool retVal = false;
            string IPAddressPattern = "^([01]?\\d\\d?|2[0-4]\\d|25[0-5])\\." + "([01]?\\d\\d?|2[0-4]\\d|25[0-5])\\." +
                                      "([01]?\\d\\d?|2[0-4]\\d|25[0-5])\\." + "([01]?\\d\\d?|2[0-4]\\d|25[0-5])$";

            int Count = CountWordOccurences(Email, "href=");

            //getting the domain name in this url (i.e in the href)
            int idx = Email.IndexOf("href="); int m = 0;
            while (Count > 0)
            {
                if (idx != -1)
                {
                    idx = Email.IndexOf("href=", ++m);
                    int idx2 = Email.IndexOf(">", idx);
                    if (idx2 != -1)
                    {
                        string url = Email.Substring(idx, idx2 - idx);
                        int idx3 = url.IndexOf("http");
                        int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
                        if (idx4 != -1)
                        {
                            string domainName = getDomainNameFromURL(url); // get the domain name
                            if (Regex.IsMatch(domainName, IPAddressPattern)) // check whether the domain name is an ip address
                            {
                                retVal = true;
                                break;
                            }
                        }
                    }
                }
                Count--;
                m = idx;
            }
            return retVal;
        }

        /// <summary>
        /// Checking emails for the presence of image tags that appears between anchor tags
        /// </summary>
        public bool imgLinkDetection(string Email)
        {
            bool retVal = false;

            int idx = Email.IndexOf("<img");

            if (idx != -1)
            {
                int idx2 = Email.IndexOf("</a>", idx); //get the index of the first occurence of </a> tag after idx
                string str = Email.Substring(idx, idx2 - idx + 4); //get the substring between the '<img' tag and '<a/>' tag
                string str2 = str.Substring(str.Length - 4, str.Length - (str.Length - 4)); //checking whether the last four characters of str is equal to "</a>" - this will always be the case if the <img tag occurs between an anchor tag(i.e btw <a> & </a>)
                if (str2.Equals("</a>"))
                    retVal = true;
            }

            return retVal;
        }

        /// <summary>
        /// This method checks for the presence of hexadecimal values in url. 
        /// Hexadecimal values such as '%' and '@' are used for redirecting users to a different website
        /// </summary>
        public bool HexAt_URLDetection(string Email)
        {
            bool retVal = false;

            //getting the domain name in this href (i.e in the url)
            int idx = Email.IndexOf("href=");

            if (idx != -1)
            {
                int idx2 = Email.IndexOf(">", idx);
                string url = Email.Substring(idx, idx2 - idx);
                int idx3 = url.IndexOf("http");
                int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
               
                string domainName = getDomainNameFromURL(url); // get the domain name
                if (domainName.Split('%').Length > 0 || domainName.Split('@').Length > 0)
                    retVal = true;
            }

            return retVal;
        }

        /// <summary>
        /// This method gets the text of each URL in each email and checks for the presence of this four words: 'click', 'login', 'update' or 'here'
        /// Also, this method, checks the url text of all the urls(that links to the modal domain) to confirm whether they contain the words indicated above
        /// Futhermore, this method checks the number of domains that is linked to, in each email
        /// Note that this method is performing three different primary tasks
        /// </summary>
        public bool getURLText(string Email, int[,] vector, string Feature, int i, int j, out int numOfDomain)
        {
            bool retVal = false; numOfDomain = 0;

            int Count = CountWordOccurences(Email, "href="); //counting the number of times "href=" occured in the mail; i.e, counting the numba of urls in the email
            int Count2 = Count;
            string[] domainNames = new string[Count2];
            int[] sum = new int[Count];

            int idx = Email.IndexOf("href=");
            int m = 0, ctr = 0;
            if (idx != -1)
            {
                int idx2 = Email.IndexOf("</a>", idx);
                while (Count > 0)
                {
                    idx = Email.IndexOf("href=", ++m);
                    idx2 = Email.IndexOf("</a>", idx);
                    if (idx2 != -1)
                    {
                        string url = Email.Substring(idx, idx2 - idx + 4);

                        int idx3 = url.IndexOf("http");
                        int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
                        if (idx4 != -1)
                        {
                            domainNames[ctr++] = getDomainNameFromURL(url); //saving the domain names to get the Modal domain

                            int idx5 = url.IndexOf(">"); int idx6 = url.IndexOf("</a>");
                            string reslt = url.Substring(idx5, idx6 - idx5);
                            int idx7 = reslt.IndexOf("http");
                            int idx8 = IndexOfNth(reslt, '/', 3, idx7);
                            if (idx8 == -1)
                            {
                                reslt = ProcessMails(reslt); //strip the text part of the url from erronoeus characters
                                if (checkURLBagWord(reslt, Feature))//checking whether the text part of the url contains either of 'click', 'login', 'update' or 'here'
                                {
                                    vector[i, j] = 1;

                                }
                            }
                        }
                    }

                    Count--;
                    m = idx;
                }

                IEnumerable<string> distint = domainNames.Distinct().Where(x => !string.IsNullOrEmpty(x)); //select the distinct urls in d email, excluding null values
                int[] add = new int[distint.Count()];
                if (Feature == "<a>" && (Count2 == 1 || Count2 == 2) && compareURLWithModalDomain(Email, "")) // check the url text of all the urls(that links to the modal domain) to confirm whether they contain these words: "link","click","here"
                {
                    retVal = true;
                }
                if (Feature == "<a>" && Count2 > 2) //check for "link", "click" and "here" in d modal domain (i.e the domain name that has d total numba of occurence in d email)
                {
                    Array.Sort(domainNames);
                    int b = 0;
                    foreach (string w in distint)
                    {
                        int cnt = 0;
                        //counting the total number of times each domain name occurs in the email
                        for (int a = 0; a < domainNames.Length; a++)
                        {
                            if (w.Equals(domainNames[a]))
                                cnt++;
                        }
                        add[b++] = cnt;
                    }
                    int max = add.Count() == 0 ? 0 : add.Max(); //get the highest value in the array, assign 0 if array contains no element
                    if (!add.Contains(max)) //ensuring that the values in the array are not equal (bcos if they are equal, then the modal domain cannot be determined)
                    {
                        int index = Array.IndexOf(add, max);
                        if (index != -1)
                        {
                            string ModalDomain = distint.ToArray()[index]; //get the domain name that has the highest number of occurence
                            if (compareURLWithModalDomain(Email, ModalDomain)) // check the url text of all the urls(that links to the modal domain) to confirm whether they contain these words: "link","click","here"
                            {
                                retVal = true;
                            }
                        }
                    }
                    else if (compareURLWithModalDomain(Email, "")) //if there is no modal domain, just check the email for the occurnce of 'link', 'click', 'here'
                        retVal = true;


                }
                else if (Feature == "linkedToDomain") //checking for the number of domains linked to in the email
                {
                    numOfDomain = distint.Count(); //assigning the total number of domains in each email
                }
            }
            return retVal;
        }

        /// <summary>
        /// This method remove erroneous characters from email
        /// </summary>
        public string[] stripChar()
        {
            string[] strpChr = new string[] { "\n", "\r", ";", "&nbsp", "<BR>", "<br>" };

            return strpChr;
        }

        /// <summary>
        /// Process email by removing any extra whitespace from email
        /// </summary>
        public string ProcessMails(string inputString)
        {
            // Convert our input to lowercase
            inputString = inputString.ToLower();
            string[] stripChars = stripChar();

            foreach (string character in stripChars)
                inputString = inputString.Replace(character, " ");

            inputString = Regex.Replace(inputString, @"\s+", " "); //Remove any extra whitespace from string
            return inputString;

        }

        /// <summary>
        /// This method performs comparism between the text part of a url and the identified ModalDomain
        /// This method is called by 'getURLText'
        /// </summary>
        public bool compareURLWithModalDomain(string Email, string ModalDomain)
        {
            bool retVal = false;
            int count = CountWordOccurences(Email, "href=");
            string[] domainNames = new string[count];
            int idx = Email.IndexOf("href=");

            int m = 0;
            if (idx != -1)
            {
                int idx2 = Email.IndexOf("</a>", idx);
                while (count > 0)
                {
                    idx = Email.IndexOf("href=", ++m);
                    idx2 = Email.IndexOf("</a>", idx);
                    if (idx2 != -1)
                    {
                        string url = Email.Substring(idx, idx2 - idx + 4);

                        int idx3 = url.IndexOf("http");
                        int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
                        if (idx4 != -1)
                        {
                            string domainName1 = getDomainNameFromURL(url); //get the domain name

                            int idx5 = url.IndexOf(">"); int idx6 = url.IndexOf("</a>");
                            string reslt = url.Substring(idx5 + 1, idx6 - idx5 - 1);
                            if (reslt.Length > 0)
                            {
                                string Feature = "link click here";
                                reslt = ProcessMails(reslt); //strip the text part of the url from erronoeus characters
                                if ((count == 1 || count == 2) && domainName1.Split('.').Length > 2 && checkURLBagWordsModalDomain(reslt, Feature))
                                {
                                    retVal = true;
                                    break;
                                }
                                else if (!domainName1.Equals(ModalDomain) && checkURLBagWordsModalDomain(reslt, Feature))//checking whether the text part of the url contains either of 'click', 'link','here'
                                {
                                    retVal = true;
                                    break;
                                }
                            }
                        }
                    }

                    count--;
                    m = idx;
                }
            }

            return retVal;
        }

        /// <summary>
        /// This method performs checks whether the url text contains 'click', 'link', or 'here'
        /// This method is called by 'getURLText'
        /// </summary>
        public bool checkURLBagWordsModalDomain(string urlText, string Feature)
        {
            bool retVal = false;
            List<string> feat = Feature.Split(' ').ToList();

            foreach (string s in feat)
            {
                if (urlText.Contains(s)) //checking whether the text part of the url contains either of 'click', 'link', or 'here'
                {
                    retVal = true;
                    break;
                }
            }
            return retVal;
        }

        /// <summary>
        /// This method checks whether an email contains the word "javascript"
        /// This word usually indicates that an email contains javascript code
        /// </summary>
        public bool containsJavaScript(string Email)
        {
            bool retVal = false;
            int idx = Email.ToLower().IndexOf("javascript"); //checking whether the email contains javascript codes in it

            if (idx != -1)
            {
                retVal = true;
            }

            return retVal;
        }

        /// <summary>
        /// This method counts the number of links in each email
        /// </summary>
        public int countNumberOfLinks(string email)
        {
            int count = CountWordOccurences(email, "href="); //counting the number of times "href=" occured in the mail; i.e, counting the numba of urls in the email
            return count;
        }

        /// <summary>
        /// checking  domain name mismatch between the sender's address and the domain names in the email address
        /// </summary>
        public bool checkFrom_Body_DomainNameMismatch(string mailURL, string email)
        {
            //int idDomain1 = new int(), idDomain2 = new int(), idDomain3 = new int(), idDomain4 = new int(), idDomain5 = new int();
            int idDomain1 = -1, idDomain2 = -1, idDomain3 = -1, idDomain4 = -1, idDomain5 = -1;
            string senderDomainName2 = "";
            string senderDomainName = getSenderAdress(email).Split('@').Last(); //getting the domain part of the sender's address for comparism with other domain names in the email

            if (senderDomainName.Equals(""))
            {
                idDomain1 = email.IndexOf("from:");
                if (idDomain1 != -1)
                {
                    idDomain2 = email.IndexOf("<", idDomain1);
                    idDomain3 = email.IndexOf(">", idDomain1);
                }
                if (idDomain2 != -1 && idDomain3 != -1)
                {
                    if ((idDomain3 - idDomain2) < 0)
                    {
                        idDomain4 = email.IndexOf("@", idDomain1);
                        if (idDomain4 != -1)
                            idDomain5 = email.IndexOf(" ", idDomain4);
                        senderDomainName2 = email.Substring(idDomain4 + 1, (idDomain5 - idDomain4 + 1) - 2); //getting the sender's address if the email's sender's address is not in the right format
                    }
                    else
                        senderDomainName2 = email.Substring(idDomain2 + 1, (idDomain3 - idDomain2 + 1) - 2); //getting the sender's address if the email's sender's address is not in the right format
                }
            }

            senderDomainName = (senderDomainName.Length > 0) ? senderDomainName : senderDomainName2.Split('@').Last();
            bool retVal = false;
            string[] domainNames = getDomainNames(email);
            foreach (string s in domainNames)
            {
                if (!s.Contains(senderDomainName))
                {
                    retVal = true;
                    break;
                }
            }

            return retVal;
        }

        /// <summary>
        /// This method is used to get the sender's address from an email
        /// </summary>
        public string getSenderAdress(string email)
        {
            Regex toline = new Regex(@"(?im-:^from\s*:\s*(?<from>.*)$)");
            string to = toline.Match(email).Groups["from"].Value; string address2 = "";

            int from = 0;
            int pos = 0;
            int found;
            string test = "", address = "";

            while (from < to.Length)
            {
                found = (found = to.IndexOf(',', from)) > 0 ? found : to.Length;
                from = found + 1;
                test = to.Substring(pos, found - pos);

                try
                {
                    System.Net.Mail.MailAddress addy = new System.Net.Mail.MailAddress(test.Trim());
                    address = addy.Address;
                    pos = found + 1;
                }
                catch (FormatException)
                {
                }
            }

            if (address.Equals(""))
            {
                int idDomain1 = test.IndexOf("<");
                int idDomain2 = test.IndexOf(">");
                if (idDomain1 != -1 && idDomain2 != -1)
                {
                    address2 = test.Trim().Substring(idDomain1 + 1, (idDomain2 - idDomain1 + 1) - 2); //getting the sender's address if the email's sender's address is not in the right format
                }
                address = (address.Length > 0) ? address : address2;

            }
            return address;
        }

        /// <summary>
        /// This method extract the domain names from each email for Modal Domain name processing
        /// </summary>
        public string[] getDomainNames(string Email)
        {
            IEnumerable<string> distint = new string[] { };

            int Count = CountWordOccurences(Email, "href="); //counting the number of times "href=" occured in the mail; i.e, counting the numba of urls in the email
            int Count2 = Count;
            string[] domainNames = new string[Count];
            int[] sum = new int[Count];

            int idx = Email.IndexOf("href=");
            int m = 0; int ctr = 0;
            if (idx != -1)
            {
                int idx2 = Email.IndexOf("</a>", idx);
                while (Count > 0)
                {
                    idx = Email.IndexOf("href=", ++m);
                    idx2 = Email.IndexOf("</a>", idx);
                    if (idx2 != -1)
                    {
                        string url = Email.Substring(idx, idx2 - idx + 4);

                        int idx3 = url.IndexOf("http");
                        int idx4 = IndexOfNth(url, '/', 3, idx3); //getting the index of the 3rd occurence of '/' in the string
                        if (idx4 != -1)
                        {
                            string domainName1 = getDomainNameFromURL(url); // get the domain name
                            domainNames[ctr++] = domainName1; //saving the domain names to get the Modal domain
                        }
                    }

                    Count--;
                    m = idx;
                }

                distint = domainNames.Distinct().Where(x => !string.IsNullOrEmpty(x)); //select the distinct urls in d email
            }

            return distint.ToArray();
        }

        /// <summary>
        /// This method is used to count the number of times each word feature occur in each email
        /// Note that, the count for each word feature is normalized before the result is returned
        /// </summary>
        public int checkKeyWords(string url, string feature, string email)
        {
            double normalizedEmailVal;
            List<string> word = new List<string>(); int ctr = 0; double sqrt;

            if (feature == "update")
            {
                word.Add("update"); word.Add("confirm");
            }
            else if (feature == "user")
            {
                word.Add("user"); word.Add("customer"); word.Add("client");
            }
            else if (feature == "suspend")
            {
                word.Add("suspend"); word.Add("restrict"); word.Add("hold");
            }
            else if (feature == "verify")
            {
                word.Add("verify"); word.Add("account");
            }
            else if (feature == "login")
            {
                word.Add("login"); word.Add("username"); word.Add("password");
            }
            else if (feature == "ssn")
            {
                word.Add("ssn"); word.Add("social security");
            }
            else if (feature == "bankNames")
            {
                word.Add("papal"); word.Add("visa"); word.Add("ebay");
            }

            foreach (string w in word)
            {
                ctr += CountWordOccurences(email.ToLower(), w.ToLower()); //counting the total number of times each of the words occured in the email
            }

            sqrt = Math.Sqrt(ctr);
            normalizedEmailVal = (ctr.Equals(0)) ? 0 : (double)ctr / sqrt;

            return (int)Math.Round(normalizedEmailVal);
        }

        /// <summary>
        /// This method checks url for the presence of 'click', 'login', 'update' or 'here'
        /// Note that, this method is used by 'getURLText'
        /// </summary>
        public bool checkURLBagWord(string domainName, string Feature)
        {
            bool retVal = false;
            if (domainName.Contains(Feature)) //checking whether the text part of the url contains either of 'click', 'login', 'update' or 'here'
                retVal = true;

            return retVal;
        }

        /// <summary>
        /// Calculate information gain for each feature
        /// </summary>
        public Dictionary<string, double> Feature_InfoGain(string[] feature, double[] infoGain)
        {
            Dictionary<string, double> feat_InfoGain = new Dictionary<string, double>();
            for (int i = 0; i < feature.Length; i++)
            {
                feat_InfoGain[feature[i]] = infoGain[i];
            }

            //Create a dictionary sorted by value (i.e. how many times a word occurs)
            Dictionary<string, double> sortedDict = (from entry in feat_InfoGain orderby entry.Value descending select entry).ToDictionary(pair => pair.Key, pair => pair.Value);

            return sortedDict;
        }

        /// <summary>
        /// Extract the file name (label) of each email
        /// </summary>
        public string[] getFileNames(string[] mailURLs)
        {
            string[] testMailFileNames = new string[mailURLs.Length];
            for (int j = 0; j < mailURLs.Length; j++)
            {
                testMailFileNames[j] = mailURLs[j].Split('\\').Last(); //save the file names for all the test mails
            }

            return testMailFileNames;
        }

        /// <summary>
        /// Calculate the vector sum of each phish and ham email
        /// The vector sum is needed to calculate information gain
        /// </summary>
        public void FeatureVectorSum(int FNum, int EmailCount, int[,] vector, Dictionary<string, int> emails, int[,] HamPhishCount)
        {
            for (int i = 0; i < FNum; i++)
            {
                int sumZeroHam = 0, sumOneHam = 0, sumZeroPhish = 0, sumOnePhish = 0; int k = 0;
                for (int j = 0; j < EmailCount; j++)
                {
                    if (vector[j, i].Equals(0) && emails.ElementAt(j).Value.Equals(0))
                        sumZeroHam++;
                    if (vector[j, i].Equals(0) && emails.ElementAt(j).Value.Equals(1))
                        sumZeroPhish++;
                    if (vector[j, i] > 0 && emails.ElementAt(j).Value.Equals(0))
                        sumOneHam += vector[j, i];
                    if (vector[j, i] > 0 && emails.ElementAt(j).Value.Equals(1))
                        sumOnePhish += vector[j, i];
                }


                HamPhishCount[i, k] = sumZeroHam; //total number of zeros for ham mails - zero indicate that a feature is absent in a mail and one indicate otherwise
                HamPhishCount[i, k + 1] = sumZeroPhish; //total number of zeros for Phishing emails
                HamPhishCount[i, k + 2] = sumOneHam; //total number of ones for ham mails
                HamPhishCount[i, k + 3] = sumOnePhish; //total number of ones for Phishing mails
            }
        }

        /// <summary>
        /// Calculate the vector sum of credit card fraud
        /// The vector sum is needed to calculate information gain
        /// </summary>
        public void FeatureVectorSumCreditCard(int FNum, int EmailCount, double[,] vector, Dictionary<string, int> emails, double[,] HamPhishCount)
        {
            for (int i = 0; i < FNum; i++)
            {
                double sumZeroHam = 0, sumOneHam = 0, sumZeroPhish = 0, sumOnePhish = 0; int k = 0;
                for (int j = 0; j < EmailCount; j++)
                {
                    if (vector[j, i].Equals(0) && emails.ElementAt(j).Value.Equals(0))
                        sumZeroHam++;
                    if (vector[j, i].Equals(0) && emails.ElementAt(j).Value.Equals(1))
                        sumZeroPhish++;
                    if (vector[j, i] > 0 && emails.ElementAt(j).Value.Equals(0))
                        sumOneHam += vector[j, i];
                    if (vector[j, i] > 0 && emails.ElementAt(j).Value.Equals(1))
                        sumOnePhish += vector[j, i];
                }


                HamPhishCount[i, k] = sumZeroHam; //total number of zeros for ham mails - zero indicate that a feature is absent in a mail and one indicate otherwise
                HamPhishCount[i, k + 1] = sumZeroPhish; //total number of zeros for Phishing emails
                HamPhishCount[i, k + 2] = sumOneHam; //total number of ones for ham mails
                HamPhishCount[i, k + 3] = sumOnePhish; //total number of ones for Phishing mails
            }
        }

        /// <summary>
        /// This method is used to extract vector value, format the vector value and send the formatted vector value to file
        /// </summary>
        public void extractVectors(int[,] vector, Dictionary<string, int> Mail_Class, int NumofFeatures, string extractedFeaturePath, int fileNum)
        {
            string vectorSVM = "";  string formattedVector;
            extractedFeaturePath = (extractedFeaturePath.Equals("trainingDS")) ? extractedFeaturesFilePathTrainDS : extractedFeaturesFilePathTestDS;
            extractedFeaturePath = string.Format("{0}{1}.{2}", extractedFeaturePath.Split('.').First(),(fileNum + 1).ToString(), "txt");
           
            File.WriteAllText(extractedFeaturePath, string.Empty); //clearing the file
            
            //extract vectors for training data
            for (int i = 0; i < vector.GetLength(0); i++)
            {
                int index = 0;
                vectorSVM = Mail_Class.ElementAt(i).Value.Equals(0) ? "-1" : "+1"; //assigning labels to the feature vectors
                File.AppendAllText(extractedFeaturePath, vectorSVM + " ");
                for (int j = 0; j < NumofFeatures; j++)
                {
                    formattedVector = string.Format("{0}:{1}", ++index, vector[i, j]);
                    File.AppendAllText(extractedFeaturePath, formattedVector + " ");
                }

                File.AppendAllText(extractedFeaturePath, Environment.NewLine);
            }

        }
    }
}
