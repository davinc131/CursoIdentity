﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Identity.DTO
{
  public class UserLoginDto
  {
    public string UserName { get; set; }
    public string Password { get; set; }
  }
}
