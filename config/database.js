const mysql = require("mysql2");
require("dotenv").config();

const pool = mysql
  .createPool({
    host: process.env.DB_HOST || "localhost",
    user: process.env.DB_USER || "root",
    password: process.env.DB_PASSWORD || "your_password", // Replace with your actual MySQL password
    database: process.env.DB_NAME || "careconnect",
    waitForConnections: true,
    connectionLimit: 10,
    queueLimit: 0,
  })
  .promise();

// Test connection
pool
  .query("SELECT 1")
  .then(() => console.log("✅ Database connected successfully!"))
  .catch((err) => console.error("Database connection error:", err));

module.exports = pool;
