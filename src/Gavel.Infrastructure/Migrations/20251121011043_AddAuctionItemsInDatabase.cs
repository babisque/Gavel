using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gavel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionItemsInDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO [AuctionItems] 
                ([Id], [Name], [Description], [InitialPrice], [CurrentPrice], [StartTime], [EndTime], [Status])
                VALUES
                /* 1. High-end Electronics */
                (NEWID(), 'MacBook Pro M3 Max', '16-inch, 1TB SSD, 32GB RAM, Space Black. Barely used.', 2500.00, 2650.00, '2023-11-01 10:00:00', '2023-11-10 10:00:00', 2),
                (NEWID(), 'Sony A7IV Camera', 'Mirrorless camera body with 28-70mm lens kit.', 1800.00, 1800.00, '2024-05-20 09:00:00', '2024-05-27 18:00:00', 1),
                (NEWID(), 'PlayStation 5 Pro', 'Limited edition bundle with two controllers.', 499.00, 550.00, '2024-05-21 12:00:00', '2024-05-24 12:00:00', 1),
                (NEWID(), 'Samsung Odyssey G9', '49-inch curved gaming monitor, 240Hz.', 900.00, 1100.00, '2023-12-01 08:00:00', '2023-12-05 20:00:00', 2),
                (NEWID(), 'DJI Mavic 3 Drone', 'Fly More Combo, includes 3 batteries and bag.', 1500.00, 1525.00, '2024-05-22 14:00:00', '2024-05-29 14:00:00', 1),

                /* 2. Musical Instruments */
                (NEWID(), 'Gibson Les Paul Standard', '1959 Reissue, Cherry Sunburst finish. Excellent condition.', 3500.00, 4200.00, '2024-05-15 10:00:00', '2024-05-25 10:00:00', 1),
                (NEWID(), 'Fender Stratocaster', 'American Professional II, Olympic White.', 1200.00, 1200.00, '2024-05-22 08:00:00', '2024-05-30 18:00:00', 1),
                (NEWID(), 'Roland TD-17KVX', 'Electronic Drum Set with Bluetooth.', 1100.00, 1150.00, '2024-02-10 09:00:00', '2024-02-17 09:00:00', 2),

                /* 3. Collectibles & Antiques */
                (NEWID(), '1921 Morgan Silver Dollar', 'Rare coin, graded MS65 by PCGS.', 150.00, 320.00, '2024-05-18 11:00:00', '2024-05-23 11:00:00', 1),
                (NEWID(), 'Vintage Typewriter', '1940s Royal Quiet Deluxe, working condition.', 80.00, 120.00, '2024-01-15 10:00:00', '2024-01-20 10:00:00', 2),
                (NEWID(), 'First Edition Comic Book', 'Spider-Man #1 (1990), Todd McFarlane cover.', 40.00, 95.00, '2024-05-20 15:00:00', '2024-05-27 15:00:00', 1),
                (NEWID(), 'Antique Brass Telescope', 'Maritime telescope with wooden tripod stand.', 200.00, 200.00, '2024-05-22 16:00:00', '2024-06-01 12:00:00', 1),

                /* 4. Fashion & Accessories */
                (NEWID(), 'Rolex Submariner', 'Stainless steel, black dial, no date. Box and papers included.', 8500.00, 9100.00, '2024-05-10 08:00:00', '2024-05-24 20:00:00', 1),
                (NEWID(), 'Louis Vuitton Keepall', 'Keepall Bandoulière 55, Monogram Canvas.', 1200.00, 1350.00, '2024-03-01 10:00:00', '2024-03-08 10:00:00', 2),
                (NEWID(), 'Air Jordan 1 High', 'Lost and Found Chicago colorway, Size 10.', 180.00, 350.00, '2024-05-19 09:00:00', '2024-05-26 09:00:00', 1),
                (NEWID(), 'Ray-Ban Aviator', 'Classic gold frame with G-15 green lenses.', 90.00, 110.00, '2024-04-05 12:00:00', '2024-04-12 12:00:00', 2),

                /* 5. Home & Office */
                (NEWID(), 'Herman Miller Aeron', 'Size B, Graphite color, fully loaded features.', 600.00, 750.00, '2024-05-21 08:30:00', '2024-05-28 08:30:00', 1),
                (NEWID(), 'Espresso Machine', 'Breville Barista Express, stainless steel.', 400.00, 450.00, '2024-05-01 10:00:00', '2024-05-05 10:00:00', 2),
                (NEWID(), 'Dyson V15 Detect', 'Cordless vacuum cleaner with laser detect.', 500.00, 500.00, '2024-05-22 11:00:00', '2024-05-29 11:00:00', 1),
                (NEWID(), 'Standing Desk Frame', 'Dual motor, electric height adjustable, black frame.', 250.00, 280.00, '2024-05-15 14:00:00', '2024-05-22 14:00:00', 1);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM [AuctionItems] WHERE [Name] IN (
                    'MacBook Pro M3 Max', 'Sony A7IV Camera', 'PlayStation 5 Pro', 'Samsung Odyssey G9', 'DJI Mavic 3 Drone',
                    'Gibson Les Paul Standard', 'Fender Stratocaster', 'Roland TD-17KVX',
                    '1921 Morgan Silver Dollar', 'Vintage Typewriter', 'First Edition Comic Book', 'Antique Brass Telescope',
                    'Rolex Submariner', 'Louis Vuitton Keepall', 'Air Jordan 1 High', 'Ray-Ban Aviator',
                    'Herman Miller Aeron', 'Espresso Machine', 'Dyson V15 Detect', 'Standing Desk Frame'
                );
            ");
        }
    }
}