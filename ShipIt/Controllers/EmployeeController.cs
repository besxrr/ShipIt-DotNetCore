﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;

namespace ShipIt.Controllers
{

    [Route("employees")]
    public class EmployeeController : ControllerBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        [HttpGet("")]
        public EmployeeResponse Get([FromQuery] int id)
        {
            Log.Info($"Looking up employee by id: {id}");

            var employee = new Employee(_employeeRepository.GetEmployeeById(id));

            Log.Info("Found employee: " + employee);
            return new EmployeeResponse(employee);
        }
        

        [HttpPost("")]
        public Response Post([FromBody] AddEmployeesRequest requestModel)
        {
            List<Employee> employeesToAdd = requestModel.Employees;

            if (employeesToAdd.Count == 0)
            {
                throw new MalformedRequestException("Expected at least one <employee> tag");
            }

            Log.Info("Adding employees: " + employeesToAdd);

            _employeeRepository.AddEmployees(employeesToAdd);

            Log.Debug("Employees added successfully");

            return new Response() { Success = true };
        }

        [HttpDelete("")]
        public void Delete([FromBody] RemoveEmployeeRequest requestModel)
        {
            int id = requestModel.Id;
            if (id == null)
            {
                throw new MalformedRequestException("Unable to parse id from request parameters");
            }

            try
            {
                _employeeRepository.RemoveEmployee(id);
            }
            catch (NoSuchEntityException)
            {
                throw new NoSuchEntityException("No employee exists with name: " + id);
            }
        }
    }
}
