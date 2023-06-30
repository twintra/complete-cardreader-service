

using System;
using System.Collections.Generic;
using System.Web.Http;
using ThaiNationalIDCard;

namespace CardReaderWindowsService
{
    public class ReaderController : ApiController
    {
        private readonly ThaiIDCard thcard = new ThaiIDCard();

        public Dictionary<String, dynamic> Get()
        {   
            Dictionary<String, dynamic> result = new Dictionary<String, dynamic>();

            string status = "ok";
            string message = null;
            string device = null;
            dynamic data = null;

            try
            {
                if (thcard.GetReaders().Length < 1) throw new Exception("device not found");

                device = thcard.GetReaders()[0];

                if (thcard.ErrorCode() > 0)
                {
                    var error = thcard.Error();
                    message = error.Substring(error.LastIndexOf('(')+1, error.Length-1);
                }
                else
                {
                    var personal = thcard.readAll(true);
                    if (personal == null)
                    {
                        message = "not_insert";
                    }
                    else
                    {
                        Dictionary<String, String> ps = new Dictionary<String, String>
                        {
                            { "cid", personal.Citizenid },
                            { "birthday", personal.Birthday?.ToString("dd/MM/yyyy") },
                            { "sex", personal.Sex },
                            { "th_prefix", personal.Th_Prefix },
                            { "th_firstname", personal.Th_Firstname },
                            { "th_lastname", personal.Th_Lastname },
                            { "en_prefix", personal.En_Prefix },
                            { "en_firstname", personal.En_Firstname },
                            { "en_lastname", personal.En_Lastname },
                            { "issue", personal.Issue.ToString("dd/MM/yyyy") },
                            { "expire", personal.Expire.ToString("dd/MM/yyyy") },
                            { "address", personal.Address },
                            { "addr_house_no", personal.addrHouseNo },
                            { "addr_village_no", personal.addrVillageNo },
                            { "addr_lane", personal.addrLane },
                            { "addr_road", personal.addrRoad },
                            { "addr_tambol", personal.addrTambol },
                            { "addr_amphur", personal.addrAmphur },
                            { "addr_province", personal.addrProvince },
                            { "photo", Convert.ToBase64String(personal.PhotoRaw) }
                        };

                        message = "inserted";
                        data = ps;
                    }
                }

            }
            catch (Exception e)
            {
                status = "error";
                message = e.Message;
            }

            result.Add("status", status);
            result.Add("message", message);
            result.Add("device", device);
            result.Add("data", data);

            return result;
        }

    }
}