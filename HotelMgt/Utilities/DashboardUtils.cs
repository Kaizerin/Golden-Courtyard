using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace HotelMgt.Utilities
{
    public static class DashboardUtils
    {
        public static DataTable GetGuestOccupancyData(SqlConnection conn)
        {
            const string query = @"
                SELECT
                    rm.RoomNumber AS [Room],
                    (g.FirstName +
                     CASE WHEN g.MiddleName IS NOT NULL AND g.MiddleName <> '' THEN ' ' + g.MiddleName ELSE '' END +
                     ' ' + g.LastName) AS [GuestName],
                    g.PhoneNumber AS [Contact],
                    CAST(c.CheckInDateTime AS DATE) AS [Check In],
                    c.ExpectedCheckOutDate AS [Expected Check Out],
                    c.NumberOfGuests AS [Guests],
                    COALESCE(STRING_AGG(a.Name, ', '), '') AS [Amenities],
                    ISNULL(c.Notes, '') AS [Notes]
                FROM CheckIns c
                INNER JOIN Rooms rm ON c.RoomID = rm.RoomID
                INNER JOIN Guests g ON c.GuestID = g.GuestID
                LEFT JOIN CheckInAmenities cia ON cia.CheckInID = c.CheckInID
                LEFT JOIN Amenities a ON a.AmenityID = cia.AmenityID
                WHERE c.ActualCheckOutDateTime IS NULL
                GROUP BY
                    rm.RoomNumber,
                    (g.FirstName +
                     CASE WHEN g.MiddleName IS NOT NULL AND g.MiddleName <> '' THEN ' ' + g.MiddleName ELSE '' END +
                     ' ' + g.LastName),
                    g.PhoneNumber,
                    CAST(c.CheckInDateTime AS DATE),
                    c.ExpectedCheckOutDate,
                    c.NumberOfGuests,
                    ISNULL(c.Notes, '')
                ORDER BY MAX(c.CheckInDateTime) DESC";

            var dt = new DataTable();
            using var adapter = new SqlDataAdapter(query, conn);
            adapter.Fill(dt);
            return dt;
        }
    }
}