﻿#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2018 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using ShareX.HistoryLib.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace ShareX.HistoryLib
{
    public class HistoryManager
    {
        private static readonly object thisLock = new object();

        public string FilePath { get; private set; }
        public string BackupFolder { get; set; }
        public bool CreateBackup { get; set; }
        public bool CreateWeeklyBackup { get; set; }

        public HistoryManager(string filePath)
        {
            FilePath = filePath;
        }

        public List<HistoryItem> GetHistoryItems()
        {
            try
            {
                return Load();
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);

                MessageBox.Show(string.Format(Resources.HistoryManager_GetHistoryItems_Error_occured_while_reading_XML_file___0_, FilePath) + "\r\n\r\n" + e,
                    "ShareX - " + Resources.HistoryManager_GetHistoryItems_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return new List<HistoryItem>();
        }

        public bool AppendHistoryItem(HistoryItem historyItem)
        {
            try
            {
                if (IsValidHistoryItem(historyItem))
                {
                    return Append(historyItem);
                }
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }

            return false;
        }

        private bool IsValidHistoryItem(HistoryItem historyItem)
        {
            return historyItem != null && !string.IsNullOrEmpty(historyItem.Filename) && historyItem.DateTime != DateTime.MinValue &&
                (!string.IsNullOrEmpty(historyItem.URL) || !string.IsNullOrEmpty(historyItem.Filepath));
        }

        private List<HistoryItem> Load()
        {
            List<HistoryItem> historyItemList = new List<HistoryItem>();

            if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
            {
                lock (thisLock)
                {
                    XmlReaderSettings settings = new XmlReaderSettings
                    {
                        ConformanceLevel = ConformanceLevel.Auto,
                        IgnoreWhitespace = true
                    };

                    using (StreamReader streamReader = new StreamReader(FilePath, Encoding.UTF8))
                    using (XmlReader reader = XmlReader.Create(streamReader, settings))
                    {
                        reader.MoveToContent();

                        while (!reader.EOF)
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "HistoryItem")
                            {
                                XElement element = XNode.ReadFrom(reader) as XElement;

                                if (element != null)
                                {
                                    HistoryItem hi = ParseHistoryItem(element);
                                    historyItemList.Add(hi);
                                }
                            }
                            else
                            {
                                reader.Read();
                            }
                        }
                    }
                }
            }

            return historyItemList;
        }

        private bool Append(params HistoryItem[] historyItems)
        {
            if (!string.IsNullOrEmpty(FilePath))
            {
                lock (thisLock)
                {
                    Helpers.CreateDirectoryFromFilePath(FilePath);

                    using (FileStream fs = File.Open(FilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (XmlTextWriter writer = new XmlTextWriter(fs, Encoding.UTF8))
                    {
                        writer.Formatting = Formatting.Indented;
                        writer.Indentation = 4;

                        foreach (HistoryItem historyItem in historyItems)
                        {
                            writer.WriteStartElement("HistoryItem");
                            writer.WriteElementIfNotEmpty("Filename", historyItem.Filename);
                            writer.WriteElementIfNotEmpty("Filepath", historyItem.Filepath);
                            writer.WriteElementIfNotEmpty("DateTimeUtc", historyItem.DateTime.ToString("o"));
                            writer.WriteElementIfNotEmpty("Type", historyItem.Type);
                            writer.WriteElementIfNotEmpty("Host", historyItem.Host);
                            writer.WriteElementIfNotEmpty("URL", historyItem.URL);
                            writer.WriteElementIfNotEmpty("ThumbnailURL", historyItem.ThumbnailURL);
                            writer.WriteElementIfNotEmpty("DeletionURL", historyItem.DeletionURL);
                            writer.WriteElementIfNotEmpty("ShortenedURL", historyItem.ShortenedURL);
                            writer.WriteEndElement();
                        }

                        writer.WriteWhitespace(Environment.NewLine);
                    }

                    if (!string.IsNullOrEmpty(BackupFolder))
                    {
                        if (CreateBackup)
                        {
                            Helpers.CopyFile(FilePath, BackupFolder);
                        }

                        if (CreateWeeklyBackup)
                        {
                            Helpers.BackupFileWeekly(FilePath, BackupFolder);
                        }
                    }
                }

                return true;
            }

            return false;
        }

        private HistoryItem ParseHistoryItem(XElement element)
        {
            HistoryItem hi = new HistoryItem();

            foreach (XElement child in element.Elements())
            {
                string name = child.Name.LocalName;

                switch (name)
                {
                    case "Filename":
                        hi.Filename = child.Value;
                        break;
                    case "Filepath":
                        hi.Filepath = child.Value;
                        break;
                    case "DateTimeUtc":
                        DateTime dateTime;
                        if (DateTime.TryParse(child.Value, out dateTime))
                        {
                            hi.DateTime = dateTime;
                        }
                        break;
                    case "Type":
                        hi.Type = child.Value;
                        break;
                    case "Host":
                        hi.Host = child.Value;
                        break;
                    case "URL":
                        hi.URL = child.Value;
                        break;
                    case "ThumbnailURL":
                        hi.ThumbnailURL = child.Value;
                        break;
                    case "DeletionURL":
                        hi.DeletionURL = child.Value;
                        break;
                    case "ShortenedURL":
                        hi.ShortenedURL = child.Value;
                        break;
                }
            }

            return hi;
        }
    }
}