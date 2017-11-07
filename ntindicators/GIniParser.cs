using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using NinjaTrader.Data;

namespace NinjaTrader.Data
{
    public class GIniParser
    {
        private Hashtable _keyPairs = new Hashtable();
        private String _iniFilePath;

        private struct _sSectionPair
        {
            public String Section;
            public String Key;
        }

        public GIniParser(String iniPath)
        {
            TextReader iniFile = null;
            String strLine = null;
            String currentRoot = null;
            String[] keyPair = null;
            String comment = "#";
            _iniFilePath = iniPath;
 
            if (File.Exists(iniPath))
            {
                try
                {
                    iniFile = new StreamReader(iniPath);
                    strLine = iniFile.ReadLine();

                    while(strLine != null)
                    {
                        strLine = strLine.Trim();

                        if(strLine != "")
                        {
                            if(strLine.StartsWith("[") && strLine.EndsWith("]"))
                            {
                                currentRoot = strLine.Substring(1, strLine.Length - 2);
                            }
                            else
                            {
                                if(!strLine.StartsWith(comment))
                                {
                                    keyPair = strLine.Split(new char[] { '=' }, 2);
 
                                    _sSectionPair sectionPair;
                                    String value = null;
 
                                    if (currentRoot == null)
                                        currentRoot = "ROOT";
 
                                    sectionPair.Section = currentRoot.ToUpper();
                                    sectionPair.Key = keyPair[0].ToUpper();
 
                                    if (keyPair.Length > 1)
                                        value = keyPair[1];

                                    _keyPairs.Add(sectionPair, value);
                                }
                            }
                        }
 
                        strLine = iniFile.ReadLine();
                    }
 
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    if (iniFile != null)
                        iniFile.Close();
                }
            }
            else
                throw new FileNotFoundException("Unable to locate " + iniPath);
        }

        public String GetSetting(String sectionName, String settingName)
        {
            _sSectionPair sectionPair;
            sectionPair.Section = sectionName.ToUpper();
            sectionPair.Key = settingName.ToUpper();

            return (String)this._keyPairs[sectionPair];
        }

        public String[] EnumSection(String sectionName)
        {
            ArrayList tmpArray = new ArrayList();

            foreach (_sSectionPair pair in this._keyPairs.Keys)
            {
                if (pair.Section == sectionName.ToUpper())
                    tmpArray.Add(pair.Key);
            }

            return (String[])tmpArray.ToArray(typeof(String));
        }

        public Dictionary<String, String> EnumSectionKeyValue(String sectionName)
        {
            Dictionary<String, String> ret = new Dictionary<String, String>();

            foreach (_sSectionPair pair in this._keyPairs.Keys)
            {
                if (pair.Section == sectionName.ToUpper())
                {
                    ret.Add(pair.Key, this.GetSetting(sectionName, pair.Key));
                }
            }

            return ret;
        }

        public Dictionary<String, String> EnumValue(String value)
        {
            Dictionary<String, String> ret = new Dictionary<String, String>();

            foreach (_sSectionPair pair in this._keyPairs.Keys)
            {
                if (this.GetSetting(pair.Section, pair.Key) == value)
                {
                    ret.Add(pair.Section, pair.Key);
                }
            }

            return ret;
        }

        public void AddSetting(String sectionName, String settingName, String settingValue)
        {
            _sSectionPair sectionPair;
            sectionPair.Section = sectionName.ToUpper();
            sectionPair.Key = settingName.ToUpper();

            if (this._keyPairs.ContainsKey(sectionPair))
                this._keyPairs.Remove(sectionPair);

            this._keyPairs.Add(sectionPair, settingValue);
        }

        public void AddSetting(String sectionName, String settingName)
        {
            AddSetting(sectionName, settingName, null);
        }

        public void DeleteSetting(String sectionName, String settingName)
        {
            _sSectionPair sectionPair;
            sectionPair.Section = sectionName.ToUpper();
            sectionPair.Key = settingName.ToUpper();

            if (this._keyPairs.ContainsKey(sectionPair))
                this._keyPairs.Remove(sectionPair);
        }

        public void SaveSettings(String newFilePath)
        {
            ArrayList sections = new ArrayList();
            String tmpValue = String.Empty;
            String strToSave = String.Empty;

            foreach (_sSectionPair sectionPair in this._keyPairs.Keys)
            {
                if (!sections.Contains(sectionPair.Section))
                    sections.Add(sectionPair.Section);
            }

            foreach (String section in sections)
            {
                strToSave += ("[" + section + "]\r\n");

                foreach (_sSectionPair sectionPair in this._keyPairs.Keys)
                {
                    if (sectionPair.Section == section)
                    {
                        tmpValue = (String)this._keyPairs[sectionPair];

                        if (tmpValue != null)
                            tmpValue = "=" + tmpValue;

                        strToSave += (sectionPair.Key + tmpValue + "\r\n");
                    }
                }

                strToSave += "\r\n";
            }

            try
            {
                TextWriter tw = new StreamWriter(newFilePath);
                tw.Write(strToSave);
                tw.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SaveSettings()
        {
            SaveSettings(_iniFilePath);
        }
    }
}
