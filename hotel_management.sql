USE `hotel-management`;
CREATE TABLE IF NOT exists `Users` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Username` VARCHAR(50) NOT NULL,
    `Password` VARCHAR(255) NOT NULL,
    `FullName` VARCHAR(50) NOT NULL,
    `Email` VARCHAR(100) NOT NULL,
    `Phone` VARCHAR(12),
    `Role` INT NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT exists `Rooms` (
    `RoomId` INT AUTO_INCREMENT PRIMARY KEY,
    `RoomNumber` VARCHAR(20) NOT NULL,
    `NumberPeople` INT NOT NULL,
    `NumberBed` INT NOT NULL,
    `Quality` INT NOT NULL,
    `BedType` VARCHAR(50) NOT NULL,
    `Price` DECIMAL(18,2) NOT NULL,
    `RoomType` VARCHAR(50) NOT NULL,
    `RoomArea` FLOAT NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT exists `Bills` (
    `BillId` INT AUTO_INCREMENT PRIMARY KEY,
    `RoomId` INT NOT NULL,
    `CreatedAt` DATETIME NOT NULL,
    `FullName` VARCHAR(50) NOT NULL,
    `Email` VARCHAR(100) NOT NULL,
    `Phone` VARCHAR(12) NOT NULL,
    `StartDate` DATETIME NOT NULL,
    `EndDate` DATETIME NOT NULL,
    `NumberPeople` INT NOT NULL,
    `Total` DECIMAL(18,2),
    `Status` INT NOT NULL,
    FOREIGN KEY (`RoomId`) REFERENCES `Rooms`(`RoomId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

