using HtmlAgilityPack;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HTMLPARSER
{
    public static class Functions
    {
        static int retries {get; set;}
        public static void ScrapeHot100(string filePath, int startYear, int endYear)
        {
            int year = startYear;
                                            //year increment
            int place = 0;                  //place increment
            Regex vte = new("vte");
            //Regex
            var file = File.AppendText(filePath);
            while (year <= endYear)
            {
                var html = $@"https://en.wikipedia.org/wiki/Billboard_Year-End_Hot_100_singles_of_{year.ToString()}";
                HtmlWeb web = new();

                var htmlDoc = web.Load(html);

                var nodes = htmlDoc.DocumentNode
                    .SelectNodes("//table/tbody/tr");
                foreach (var node in nodes)
                {
                    try
                    {
                        var split = node.InnerText.Split("\n");     //[0]empty [1]place [2]title [3]artist [4]empty
                        if (vte.IsMatch(node.InnerText) == true)
                        {
                            place = 0;
                            break;
                        }
                        if (node.InnerText != "\nNo.\n\nTitle\n\nArtist(s)\n")
                        {
                            if (split.Length < 5)
                            {
                                file.WriteLine(year + "," + (place - 1) + "," + "\"" + split[2] + "\"" + "," + split[1]);
                                place--;
                            }
                            else
                            {
                                file.WriteLine(year + "," + place + "," + "\"" + split[3] + "\"" + "," + split[2]);
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + "\n" + year + ":" + place);
                    }

                    place++;
                }

                year++;
                place = 0;
                Thread.Sleep(1000);

            }
            file.Close();
        }

        public static string GetGenreFromWikipedia(string artist)
        {
            string genre = string.Empty;
            try
            {


                string pageUrl = string.Empty;
                string searchString = $"https://en.wikipedia.org/w/index.php?search=intitle%3A{artist}";

                HtmlWeb web = new();

                var htmlDoc = web.Load(searchString);

                var nodes = htmlDoc.DocumentNode
                        .SelectNodes("/html/body/div/div/div/div/ul/li/div/a");

                Regex href = new Regex("<a href=\"/wiki/");
                foreach (var node in nodes)
                {
                    if (href.IsMatch(node.OuterHtml) == true)
                    {
                        var split = node.OuterHtml.Split("\"");
                        pageUrl = "https://en.wikipedia.org" + split[1];
                        Thread.Sleep(0);
                        break;
                    }
                }

                HtmlWeb web2 = new();

                var htmlDoc2 = web.Load(pageUrl);

                var nodes2 = htmlDoc2.DocumentNode
                        .SelectNodes("/html/body/div[3]/div[3]/div[5]/div[1]/table[1]");
                var nodes3 = htmlDoc2.DocumentNode
                        .SelectNodes("//table");
                //OuterHtml = "<div class=\"mw-search-result-heading\"><a href=\"/wiki/The_Andrews_Sisters\" title=\"The Andrews Sisters\" data-serp-pos=\"0\"><span class=\"searchmatch\">The</span> <span class=\"searchmatch\">Andrews</span> <span class=\"searchmatch\">Sisters</span></a>...
                int i = 1;
                Regex matchGenre = new("Genres");
                foreach (var node in nodes3)
                {
                    if (matchGenre.IsMatch(node.InnerHtml) == true)
                    {
                        var split = node.InnerHtml.Split("Genres");
                        var htmlDoc3 = new HtmlDocument();
                        htmlDoc3.LoadHtml(split[1]);
                        var nodes4 = htmlDoc3.DocumentNode
                            .SelectNodes("//a");
                        //var split2 = split[1].Split("\n");
                        genre = nodes4[0].InnerText;

                    }

                    i++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(artist + "\n" + ex.Message);
            }
            return genre;
        }

        public static string GetGenreFromAllMusic(string artist)
        {
            string genre = string.Empty;
            try
            {

                string pageUrl = string.Empty;
                string searchString = $"https://www.allmusic.com/search/artists/{artist}";

                HtmlWeb web = new();

                var htmlDoc = web.Load(searchString);

                var nodes = htmlDoc.DocumentNode
                        .SelectNodes("//li/div/div[contains(@class,'genres')]");

                var temp = nodes[0].InnerText.Trim();
                var temp2 = temp.Split(",");
                if (temp2.Count() > 1)
                {
                    genre = temp2[0] + "," + temp2[1];
                }
                else
                {
                    genre = temp2[0];
                }



            }
            catch (Exception ex)
            {
                Console.WriteLine(artist + "\n" + ex.Message);
                //retry
                if (retries == 0)
                {
                    retries++;
                    string[]? keywords = {"and","with",","};
                    genre = GetGenreFromAllMusic(artist.Split(keywords,2,0)[0]);
                    return genre;
                }
                
            }
            Thread.Sleep(100);
            retries = 0;
            return genre;
        }

        public static void AppendGenreToCSV(string sourcePath, string destinationPath )
        {
            using (TextFieldParser parser = new TextFieldParser(sourcePath))
            {
                var file = File.AppendText(destinationPath);
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                while (!parser.EndOfData)
                {
                    try
                    {
                        //Processing row
                        string[] fields = parser.ReadFields();
                        //Console.WriteLine("0:" + fields[0]);
                        //Console.WriteLine("1:" + fields[1]);
                        //Console.WriteLine("2:" + fields[2]);   //ARTIST
                        //Console.WriteLine("3:" + fields[3]);   //TITLE

                        var temp = fields[2].Split(" featuring ", StringSplitOptions.None);
                        //fields[2] = rgx.Replace(fields[2],". featuring *" ,"");
                        //fields[2] = rgx2.Replace(fields[2], " ");
                        //fields[2] = rgx3.Replace(fields[2], " ");
                        fields[2] = temp[0].Trim();

                        //var genre = GetGenreFromWikipedia(fields[2]);
                        var genre = GetGenreFromAllMusic($@"{fields[2]}");
                        if (genre.Length > 2)
                        {
                            file.WriteLine(fields[0] + "," + fields[1] + "," + "\"" + fields[2] + "\"" + "," + "\"" + fields[3] + "\"" + "," + genre);
                            Console.WriteLine(fields[0] + "," + fields[1] + "," + "\"" + fields[2] + "\"" + "," + "\"" + fields[3] + "\"" + "," + genre);
                        }
                        else
                        {
                            file.WriteLine(fields[0] + "," + fields[1] + "," + "\"" + fields[2] + "\"" + "," + "\"" + fields[3] + "\"");
                            Console.WriteLine(fields[0] + "," + fields[1] + "," + "\"" + fields[2] + "\"" + "," + "\"" + fields[3] + "\"");
                        }



                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                file.Close();
            }
        }
    }
}
