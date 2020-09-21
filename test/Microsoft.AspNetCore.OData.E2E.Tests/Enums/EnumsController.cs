﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Microsoft.AspNetCore.OData.E2E.Tests.Enums
{
    public class EmployeesController : ODataController
    {
        public EmployeesController()
        {
            if (null == Employees)
            {
                InitEmployees();
            }
        }

        /// <summary>
        /// static so that the data is shared among requests.
        /// </summary>
        private static IList<Employee> Employees = null;

        private void InitEmployees()
        {
            Employees = new List<Employee>
            {
                new Employee()
                {
                    ID=1,
                    Name="Name1",
                    SkillSet=new List<Skill>{Skill.CSharp,Skill.Sql},
                    Gender=Gender.Female,
                    AccessLevel=AccessLevel.Execute,
                    FavoriteSports=new FavoriteSports()
                    {
                        LikeMost=Sport.Pingpong,
                        Like=new List<Sport>{Sport.Pingpong,Sport.Basketball}
                    }
                },
                new Employee()
                {
                    ID=2,Name="Name2",
                    SkillSet=new List<Skill>(),
                    Gender=Gender.Female,
                    AccessLevel=AccessLevel.Read,
                    FavoriteSports=new FavoriteSports()
                    {
                        LikeMost=Sport.Pingpong,
                        Like=new List<Sport>{Sport.Pingpong,Sport.Basketball}
                    }
                },
                new Employee(){
                    ID=3,Name="Name3",
                    SkillSet=new List<Skill>{Skill.Web,Skill.Sql},
                    Gender=Gender.Female,
                    AccessLevel=AccessLevel.Read|AccessLevel.Write,
                    FavoriteSports=new FavoriteSports()
                    {
                        LikeMost=Sport.Pingpong|Sport.Basketball,
                        Like=new List<Sport>{Sport.Pingpong,Sport.Basketball}
                    }
                },
            };
        }

        [EnableQuery(PageSize = 10, MaxExpansionDepth = 5)]
        public IActionResult Get()
        {
            return Ok(Employees.AsQueryable());
        }

        public IActionResult Get(int key)
        {
            return Ok(Employees.SingleOrDefault(e => e.ID == key));
        }

        public IActionResult GetAccessLevelFromEmployee(int key)
        {
            return Ok(Employees.SingleOrDefault(e => e.ID == key).AccessLevel);
        }

        public IActionResult GetNameFromEmployee(int key)
        {
            return Ok(Employees.SingleOrDefault(e => e.ID == key).Name);
        }

        [EnableQuery]
        public IActionResult GetSkillSetFromEmployee(int key)
        {
            return Ok(Employees.SingleOrDefault(e => e.ID == key).SkillSet);
        }

        [EnableQuery]
        public IActionResult GetFavoriteSportsFromEmployee(int key)
        {
            var employee = Employees.SingleOrDefault(e => e.ID == key);
            return Ok(employee.FavoriteSports);
        }

        [HttpGet]
        [ODataRoute("Employees({key})/FavoriteSports/LikeMost")]
        public IActionResult GetFavoriteSportLikeMost(int key)
        {
            var firstOrDefault = Employees.FirstOrDefault(e => e.ID == key);
            return Ok(firstOrDefault.FavoriteSports.LikeMost);
        }

        public IActionResult Post([FromBody]Employee employee)
        {
            employee.ID = Employees.Count + 1;
            Employees.Add(employee);

            return Created(employee);
        }

        [ODataRoute("Employees({key})/FavoriteSports/LikeMost")]
        public IActionResult PostToSkillSet(int key, [FromBody]Skill newSkill)
        {
            Employee employee = Employees.FirstOrDefault(e => e.ID == key);
            if (employee == null)
            {
                return NotFound();
            }
            employee.SkillSet.Add(newSkill);
            return Updated(employee.SkillSet);
        }

        public IActionResult Put(int key, [FromBody]Employee employee)
        {
            employee.ID = key;
            Employee originalEmployee = Employees.SingleOrDefault(c => c.ID == key);

            if (originalEmployee == null)
            {
                Employees.Add(employee);

                return Created(employee);
            }

            Employees.Remove(originalEmployee);
            Employees.Add(employee);
            return Ok(employee);
        }

        public IActionResult Patch(int key, [FromBody]Delta<Employee> delta)
        {
            Employee originalEmployee = Employees.SingleOrDefault(c => c.ID == key);

            if (originalEmployee == null)
            {
                Employee temp = new Employee();
                delta.Patch(temp);
                Employees.Add(temp);
                return Created(temp);
            }

            delta.Patch(originalEmployee);
            return Ok(delta);
        }

        public IActionResult Delete(int key)
        {
            IEnumerable<Employee> appliedEmployees = Employees.Where(c => c.ID == key);

            if (appliedEmployees.Count() == 0)
            {
                return BadRequest(string.Format("The entry with ID {0} doesn't exist", key));
            }

            Employee employee = appliedEmployees.Single();
            Employees.Remove(employee);
            return this.StatusCode(StatusCodes.Status204NoContent);
        }

        [HttpPost]
        public IActionResult AddSkill([FromODataUri] int key, [FromBody]ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            Skill skill = (Skill)parameters["skill"];

            Employee employee = Employees.FirstOrDefault(e => e.ID == key);
            if (!employee.SkillSet.Contains(skill))
            {
                employee.SkillSet.Add(skill);
            }

            return Ok(employee.SkillSet);
        }

        [HttpPost]
        [ODataRoute("ResetDataSource")]
        public IActionResult ResetDataSource()
        {
            this.InitEmployees();
            return this.StatusCode(StatusCodes.Status204NoContent);
        }

        [HttpPost]
        [ODataRoute("SetAccessLevel")]
        public IActionResult SetAccessLevel([FromBody]ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            int ID = (int)parameters["ID"];
            AccessLevel accessLevel = (AccessLevel)parameters["accessLevel"];
            Employee employee = Employees.FirstOrDefault(e => e.ID == ID);
            employee.AccessLevel = accessLevel;
            return Ok(employee.AccessLevel);
        }

        [HttpGet]
        public IActionResult GetAccessLevel([FromODataUri] int key)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            Employee employee = Employees.FirstOrDefault(e => e.ID == key);

            return Ok(employee.AccessLevel);
        }

        [HttpGet]
        [ODataRoute("HasAccessLevel(ID={id},AccessLevel={accessLevel})")]
        public IActionResult HasAccessLevel([FromODataUri] int id, [FromODataUri] AccessLevel accessLevel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            Employee employee = Employees.FirstOrDefault(e => e.ID == id);
            var result = employee.AccessLevel.HasFlag(accessLevel);
            return Ok(result);
        }
    }

    public class WeatherForecastController : ODataController
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [EnableQuery]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Id = index,
                Status = index % 2 == 0 ? Status.SoldOut : Status.InStore,
                Skill = index % 2 == 0 ? Skill.CSharp : Skill.Sql
            })
            .ToArray();
        }
    }
}