using System;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Azure;

class Program
{
    // Connection string to your SQL Server
    static string connectionString = @"Server=.\SQLEXPRESS; Database=EmployeeDB; Integrated Security=True; TrustServerCertificate=True;";

    static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("Select an option:");
            Console.WriteLine("1. Add Employee");
            Console.WriteLine("2. View Employee");
            Console.WriteLine("3. Update Employee");
            Console.WriteLine("4. Delete Employee");
            Console.WriteLine("5. Exit");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    CreateEmployee();
                    break;
                case "2":
                    ReadEmployee();
                    break;
                case "3":
                    UpdateEmployee();
                    break;
                case "4":
                    DeleteEmployee();
                    break;
                case "5":
                    Console.WriteLine("Exiting...");
                    return;
                default:
                    Console.WriteLine("Invalid choice, try again.");
                    break;
            }
        }
    }

    // Method to Create a new Employee
    public static void CreateEmployee()
    {
        Console.WriteLine("Enter Full Name:");
        string fullName = Console.ReadLine();
        while (!IsValidName(fullName))
        {
            Console.WriteLine("Invalid name. Please enter a valid name (alphabetical characters only):");
            fullName = Console.ReadLine();
        }

        Console.WriteLine("Enter Date of Birth (yyyy-mm-dd):");
        DateTime dob = DateTime.MinValue;
        while (!IsValidDateOfBirth(Console.ReadLine(), out dob))
        {
            Console.WriteLine("Invalid date format or age is less than 18. Please enter a valid date of birth (yyyy-mm-dd):");
        }

        Console.WriteLine("Enter Salary (positive value):");
        decimal salary = 0;
        while (!decimal.TryParse(Console.ReadLine(), out salary) || salary <= 0)
        {
            Console.WriteLine("Salary must be a positive value. Please enter a valid salary:");
        }

        Console.WriteLine("Enter Civil ID (12-digit numeric, unique):");
        string civilId = Console.ReadLine();
        while (!IsValidCivilId(civilId))
        {
            Console.WriteLine("Invalid Civil ID. It must be 12 digits and unique. Please enter a valid Civil ID:");
            civilId = Console.ReadLine();
        }

        Console.WriteLine("Enter Phone Number (valid format):");
        string phoneNumber = Console.ReadLine();
        while (!IsValidPhoneNumber(phoneNumber))
        {
            Console.WriteLine("Invalid phone number. Please enter a valid phone number (e.g., +1-123-456-7890):");
            phoneNumber = Console.ReadLine();
        }

        Console.WriteLine("Enter Home Address:");
        string homeAddress = Console.ReadLine();
        while (string.IsNullOrEmpty(homeAddress))
        {
            Console.WriteLine("Home Address cannot be empty. Please enter a valid home address:");
            homeAddress = Console.ReadLine();
        }

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string query = "INSERT INTO EmployeeData (FullName, DateOfBirth, Salary, CivilID, PhoneNumber, HomeAddress) " +
                           "VALUES (@FullName, @DateOfBirth, @Salary, @CivilID, @PhoneNumber, @HomeAddress)";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@DateOfBirth", dob);
                cmd.Parameters.AddWithValue("@Salary", salary);
                cmd.Parameters.AddWithValue("@CivilID", civilId);  // Store Civil ID as plain text
                cmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);
                cmd.Parameters.AddWithValue("@HomeAddress", homeAddress);

                cmd.ExecuteNonQuery();
                Console.WriteLine("Employee added successfully.");
            }
        }
    }

    // Method to Read and display Employee info
    public static void ReadEmployee()
    {
        Console.WriteLine("Enter Employee ID to view:");
        int employeeId = int.Parse(Console.ReadLine());

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string query = "SELECT * FROM EmployeeData WHERE EmployeeID = @EmployeeID";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Retrieving data from database
                        string fullName = reader["FullName"].ToString();
                        DateTime dateOfBirth = (DateTime)reader["DateOfBirth"];
                        decimal salary = (decimal)reader["Salary"];
                        string phoneNumber = reader["PhoneNumber"].ToString();
                        string homeAddress = reader["HomeAddress"].ToString();
                        string civilId = reader["CivilID"].ToString();

                        // Calculate age
                        int age = DateTime.Now.Year - dateOfBirth.Year;
                        if (DateTime.Now.Month < dateOfBirth.Month ||
                            (DateTime.Now.Month == dateOfBirth.Month && DateTime.Now.Day < dateOfBirth.Day))
                        {
                            age--;
                        }

                        // Split Full Name into components (assumes name format is "FirstName Father'sName FamilyName")
                        string[] nameParts = fullName.Split(' ');

                        string firstName = nameParts.Length > 0 ? nameParts[0] : "Unknown";
                        string secondName = nameParts.Length > 1 ? nameParts[1] : "Unknown";
                        string familyName = nameParts.Length > 2 ? nameParts[2] : "Unknown";
                        string maskedCivilID = MaskCivilId(civilId);

                        // Output the formatted information
                        Console.WriteLine($"Hello! My name is {firstName}, my father's name is {secondName}, and my family name is {familyName}. " +
                                           $"My age is {age}, and I receive a salary of {salary} KWD a month. " +
                             
                                           $"I live in {homeAddress} and my phone number is {phoneNumber}. My Civil ID is {maskedCivilID}.");
                    }
                    else
                    {
                        Console.WriteLine("Employee not found.");
                    }
                }
            }
        }
    }


    // Method to Update Employee data
    public static void UpdateEmployee()
    {
        Console.WriteLine("Enter Employee ID to update:");
        int employeeId = int.Parse(Console.ReadLine());

        Console.WriteLine("Enter new Full Name:");
        string fullName = Console.ReadLine();
        while (!IsValidName(fullName))
        {
            Console.WriteLine("Invalid name. Please enter a valid name (alphabetical characters only):");
            fullName = Console.ReadLine();
        }

        Console.WriteLine("Enter new Salary:");
        decimal salary = 0;
        while (!decimal.TryParse(Console.ReadLine(), out salary) || salary <= 0)
        {
            Console.WriteLine("Salary must be a positive value. Please enter a valid salary:");
        }

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string query = "UPDATE EmployeeData SET FullName = @FullName, Salary = @Salary WHERE EmployeeID = @EmployeeID";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Salary", salary);
                cmd.Parameters.AddWithValue("@EmployeeID", employeeId);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    Console.WriteLine("Employee updated successfully.");
                }
                else
                {
                    Console.WriteLine("Employee not found.");
                }
            }
        }
    }

    // Method to Delete Employee data
    public static void DeleteEmployee()
    {
        Console.WriteLine("Enter Employee ID to delete:");
        int employeeId = int.Parse(Console.ReadLine());

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string query = "DELETE FROM EmployeeData WHERE EmployeeID = @EmployeeID";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeID", employeeId);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    Console.WriteLine("Employee deleted successfully.");
                }
                else
                {
                    Console.WriteLine("Employee not found.");
                }
            }
        }
    }

    // Validation Methods

    public static bool IsValidName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && Regex.IsMatch(name, @"^[a-zA-Z\s]+$");
    }

    public static bool IsValidDateOfBirth(string dobInput, out DateTime dob)
    {
        if (DateTime.TryParse(dobInput, out dob))
        {
            int age = DateTime.Now.Year - dob.Year;
            if (DateTime.Now.Month < dob.Month || (DateTime.Now.Month == dob.Month && DateTime.Now.Day < dob.Day))
            {
                age--;
            }
            return age >= 18;
        }
        return false;
    }

    public static bool IsValidSalary(decimal salary)
    {
        return salary > 0;
    }

    public static bool IsValidCivilId(string civilId)
    {
        // Check if Civil ID is numeric and exactly 12 digits
        if (string.IsNullOrWhiteSpace(civilId) || !Regex.IsMatch(civilId, @"^\d{12}$"))
            return false;

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string query = "SELECT COUNT(*) FROM EmployeeData WHERE CivilID = @CivilID";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CivilID", civilId);
                int count = (int)cmd.ExecuteScalar();
                return count == 0;  // Civil ID should be unique
            }
        }
    }

    public static bool IsValidPhoneNumber(string phoneNumber)
    {
        return Regex.IsMatch(phoneNumber, @"^\+?[0-9]{1,3}-?[0-9]{1,4}-?[0-9]{1,4}$");
    }

    // Method to mask the Civil ID for privacy
    public static string MaskCivilId(string civilId)
    {
        return new string('*', civilId.Length - 4) + civilId.Substring(civilId.Length - 1); // Mask all but the last 4 digits
    }
}
