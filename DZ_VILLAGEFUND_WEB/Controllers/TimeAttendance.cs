using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;
using System.Linq;

namespace DZ_VILLAGEFUND_WEB.Controllers
{
    public class TimeAttendance : Controller
    {
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;

        // GET: Day
        [HttpGet]
        public IActionResult GetDay(int Day, int Month, int Year)
        {
            string Msg = "";
            try
            {
                List<SelectListItem> _Ddays = new List<SelectListItem>();
                int Ddays = DateTime.DaysInMonth(Year, Month);
                for (int i = 1; i <= Ddays; i++)
                {
                    var DT = new DateTime(Year, Month, i);
                    bool IsHoliDay = false;


                    if (Day == i)
                    {
                        _Ddays.Add(new SelectListItem() { Text = i.ToString(), Value = i.ToString(), Selected = true, Disabled = IsHoliDay });

                    }
                    else
                    {
                        if (i == Ddays && Day > Ddays)
                        {
                            _Ddays.Add(new SelectListItem() { Text = i.ToString(), Value = i.ToString(), Selected = true, Disabled = IsHoliDay });
                        }
                        else
                        {
                            _Ddays.Add(new SelectListItem() { Text = i.ToString(), Value = i.ToString(), Disabled = IsHoliDay });
                        }
                    }

                }

                //Select Date Default
                if (Day == 0 && Month == 1)
                {
                    foreach (var item in _Ddays)
                    {
                        if (item.Value == DateTime.Now.Day.ToString())
                        {
                            item.Selected = true;

                        }
                    }
                }


                ViewBag.Day = _Ddays; /*new SelectList(_Ddays, "Value", "Text");*/
            }
            catch (Exception e)
            {
                Msg = "Error is :" + e.InnerException;
            }
            return Json(ViewBag.Day);
        }
    }
}
