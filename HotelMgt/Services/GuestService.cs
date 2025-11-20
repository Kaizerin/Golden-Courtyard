using System;
using Microsoft.Data.SqlClient;

namespace HotelMgt.Services
{
    public class GuestService
    {
        public int EnsureGuest(
            SqlConnection conn,
            SqlTransaction tx,
            string firstName,
            string middleName, 
            string lastName,
            string phone,
            string email,
            string idType,
            string idNumber)
        {
            // Example: match by name and phone, or by ID number
            using (var find = new SqlCommand(@"
                SELECT TOP 1 GuestID FROM Guests
                WHERE LOWER(FirstName) = LOWER(@FirstName)
                  AND LOWER(ISNULL(MiddleName, '')) = LOWER(ISNULL(@MiddleName, ''))
                  AND LOWER(LastName) = LOWER(@LastName)
                  AND (PhoneNumber = @Phone OR IDNumber = @IDNumber);", conn, tx))
            {
                find.Parameters.AddWithValue("@FirstName", firstName.Trim());
                find.Parameters.AddWithValue("@MiddleName", string.IsNullOrWhiteSpace(middleName) ? "" : middleName.Trim());
                find.Parameters.AddWithValue("@LastName", lastName.Trim());
                find.Parameters.AddWithValue("@Phone", phone.Trim());
                find.Parameters.AddWithValue("@IDNumber", idNumber.Trim());
                var existing = find.ExecuteScalar();
                if (existing is int id) return id;
            }

            using (var insert = new SqlCommand(@"
INSERT INTO Guests (FirstName, MiddleName, LastName, Email, PhoneNumber, IDNumber, IDType, CreatedAt, UpdatedAt)
VALUES (@FirstName, @MiddleName, @LastName, NULLIF(@Email,''), @Phone, @IDNumber, @IDType, GETDATE(), GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS int);", conn, tx))
            {
                insert.Parameters.AddWithValue("@FirstName", firstName.Trim());
                insert.Parameters.AddWithValue("@MiddleName", string.IsNullOrWhiteSpace(middleName) ? (object)DBNull.Value : middleName.Trim());
                insert.Parameters.AddWithValue("@LastName", lastName.Trim());
                insert.Parameters.AddWithValue("@Email", email.Trim());
                insert.Parameters.AddWithValue("@Phone", phone.Trim());
                insert.Parameters.AddWithValue("@IDNumber", idNumber.Trim());
                insert.Parameters.AddWithValue("@IDType", idType.Trim());
                return (int)insert.ExecuteScalar()!;
            }
        }

        public bool HasActiveReservation(SqlConnection conn, SqlTransaction? tx, int guestId)
        {
            using var cmd = new SqlCommand(@"
        SELECT COUNT(*) FROM Reservations
        WHERE GuestID = @GuestID
          AND ReservationStatus = 'Confirmed'
    ", conn, tx);
            cmd.Parameters.AddWithValue("@GuestID", guestId);
            var count = (int)cmd.ExecuteScalar();
            return count > 0;
        }

        public int? FindGuestId(SqlConnection conn, SqlTransaction? tx, string firstName, string middleName, string lastName, string phone, string email)
        {
            using var cmd = new SqlCommand(@"
        SELECT TOP 1 GuestID FROM Guests
        WHERE FirstName = @FirstName AND LastName = @LastName
          AND (@MiddleName IS NULL OR MiddleName = @MiddleName)
          AND (PhoneNumber = @Phone OR Email = @Email)
    ", conn, tx);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@MiddleName", string.IsNullOrWhiteSpace(middleName) ? (object)DBNull.Value : middleName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            cmd.Parameters.AddWithValue("@Phone", phone);
            cmd.Parameters.AddWithValue("@Email", email);
            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? null : (int?)result;
        }
    }
}