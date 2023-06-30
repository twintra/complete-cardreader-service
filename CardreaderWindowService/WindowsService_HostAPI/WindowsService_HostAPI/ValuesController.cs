using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ThaiNationalIDCard;

namespace CardReaderWindowsService
{
    public class ValuesController : ApiController
    {
        public Dictionary<String, String> GetData(String value)
        {   
            Dictionary<String, String> data = new Dictionary<String, String>();
            data = ReadCard();

            if (value != null)
            {
                if(value == "all") return data;

                try
                {
                    if (data[value] != null)
                    {
                        Dictionary<String, String> returnValue = new Dictionary<string, string>();
                        returnValue.Add(value, data[value]);

                        return returnValue;
                    }

                }
                catch (KeyNotFoundException)
                {
                    Dictionary<String, String> returnValue = new Dictionary<string, string>();
                    returnValue.Add("error", "No required value");
                    return returnValue;

                }
                
            }
            return data;
        }

        private Dictionary<String, String> ReadCard()
        {
            var idcard = new ThaiIDCard();
            var personal = idcard.readAll(true);
            Dictionary<String, String> data = new Dictionary<String, String>();
            if (personal != null)
            {

                String string_photo = Convert.ToBase64String(personal.PhotoRaw);

                data.Add("cid", personal.Citizenid);
                data.Add("birthday", personal.Birthday?.ToString("dd/MM/yyyy"));
                data.Add("sex",personal.Sex);
                data.Add("th_prefix", personal.Th_Prefix);
                data.Add("th_firstname", personal.Th_Firstname);
                data.Add("th_lastname", personal.Th_Lastname);
                data.Add("en_prefix", personal.En_Prefix);
                data.Add("en_firstname", personal.En_Firstname);
                data.Add("en_lastname", personal.En_Lastname);
                data.Add("issue", personal.Issue.ToString("dd/MM/yyyy"));
                data.Add("expire", personal.Expire.ToString("dd/MM/yyyy"));
                data.Add("address", personal.Address);
                data.Add("addr_house_no", personal.addrHouseNo);
                data.Add("addr_village_no", personal.addrVillageNo);
                data.Add("addr_lane", personal.addrLane);
                data.Add("addr_road", personal.addrRoad);
                data.Add("addr_tambol", personal.addrTambol);
                data.Add("addr_amphur", personal.addrAmphur);
                data.Add("addr_province", personal.addrProvince);
                data.Add("photo", string_photo);


            }
            else if (idcard.ErrorCode() > 0)
            {
                data.Add("error", "error : " + idcard.Error());
            }
            return data;
        }



    }
}