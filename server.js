const express = require("express");
const cors = require("cors");
const path = require("path");
const mysql = require("mysql2/promise");
require("dotenv").config();

// Create database connection pool
const db = mysql.createPool({
  host: process.env.DB_HOST,
  user: process.env.DB_USER,
  password: process.env.DB_PASSWORD,
  database: process.env.DB_NAME,
  waitForConnections: true,
  connectionLimit: 10,
  queueLimit: 0,
});
const router = express.Router();
const pool = require("./config/database");
const app = express();

// Test database connection
db.getConnection()
  .then((connection) => {
    console.log("Database connected successfully");
    connection.release();
  })
  .catch((error) => {
    console.error("Error connecting to the database:", error);
  });

// Middleware
app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(express.static("public"));

// Routes

// Database connection test route
app.get("/api/test", async (req, res) => {
  try {
    const [rows] = await db.query("SELECT 1");
    res.json({
      message: "Database connection successful",
      data: rows,
      timestamp: new Date(),
    });
  } catch (error) {
    console.error("Database test error:", error);
    res.status(500).json({
      message: "Database connection failed",
      error: error.message,
    });
  }
});

// Get current counts
app.get("/api/counts", async (req, res) => {
  try {
    const [rows] = await db.query(
      `SELECT in_house, new_admissions 
       FROM counts 
       ORDER BY created_at DESC 
       LIMIT 1`
    );
    console.log("Fetched rows:", rows);

    if (rows.length === 0) {
      console.log("No counts found, returning defaults");
      return res.json({ in_house: 0, new_admissions: 0 });
    }
    console.log("Returning counts:", rows[0]);
    res.json(rows[0]);
  } catch (error) {
    console.error("Error fetching counts:", error);
    res.status(500).json({
      success: false,
      message: "Error fetching counts",
      error: error.message,
    });
  }
});

// Update counts
app.post("/api/update-counts", async (req, res) => {
  try {
    console.log("Received update request:", req.body);
    const { inHouse, newAdmissions } = req.body;

    // Input validation
    if (typeof inHouse !== "number" || typeof newAdmissions !== "number") {
      console.log("Invalid input types:", { inHouse, newAdmissions });
      return res.status(400).json({
        success: false,
        message: "Invalid input: values must be numbers",
      });
    }

    if (inHouse < 0 || newAdmissions < 0) {
      return res.status(400).json({
        success: false,
        message: "Invalid input: values cannot be negative",
      });
    }

    const [result] = await db.query(
      `INSERT INTO counts (in_house, new_admissions) 
       VALUES (?, ?)`,
      [inHouse, newAdmissions]
    );
    console.log("Update successful:", result);

    res.json({
      success: true,
      message: "Counts updated successfully",
      data: {
        id: result.insertId,
        in_house: inHouse,
        new_admissions: newAdmissions,
      },
    });
  } catch (error) {
    console.error("Error updating counts:", error);
    res.status(500).json({
      success: false,
      message: "Error updating counts",
      error: error.message,
    });
  }
});

// Get all staff members
app.get("/api/staff", async (req, res) => {
  try {
    const [rows] = await db.query("SELECT * FROM staff ORDER BY name");
    res.json(rows);
  } catch (error) {
    console.error("Error fetching staff:", error);
    res.status(500).json({ error: "Failed to fetch staff" });
  }
});

// Get acknowledgments for a specific info
app.get("/api/acknowledgments/:infoId", async (req, res) => {
  try {
    const [rows] = await db.query(
      `SELECT a.acknowledged_at, s.name as staff_name
       FROM tempstaff a
       JOIN staff s ON a.staff_id = s.id
       WHERE a.info_id = ?
       ORDER BY a.acknowledged_at DESC`,
      [req.params.infoId]
    );
    res.json(rows);
  } catch (error) {
    console.error("Error fetching acknowledgments:", error);
    res.status(500).json({ error: "Failed to fetch acknowledgments" });
  }
});

// Get acknowledgment status
app.get("/api/acknowledgment-status/:infoId", async (req, res) => {
  try {
    const [staffCount] = await db.query("SELECT COUNT(*) as total FROM staff");
    const [ackCount] = await db.query(
      "SELECT COUNT(DISTINCT staff_id) as count FROM tempstaff WHERE info_id = ?",
      [req.params.infoId]
    );

    const isFullyAcknowledged = ackCount[0].count === staffCount[0].total;

    res.json({
      isFullyAcknowledged,
      totalStaff: staffCount[0].total,
      acknowledgedCount: ackCount[0].count,
    });
  } catch (error) {
    console.error("Error checking acknowledgment status:", error);
    res.status(500).json({ error: "Failed to check acknowledgment status" });
  }
});

// Acknowledge endpoint
app.post("/api/acknowledge", async (req, res) => {
  try {
    const { infoId, staffId } = req.body;

    // Input validation
    if (!infoId || !staffId) {
      return res.status(400).json({
        success: false,
        message: "Missing required fields",
      });
    }

    await db.query(
      `INSERT INTO tempstaff (info_id, staff_id, acknowledged_at) 
       VALUES (?, ?, NOW())`,
      [infoId, staffId]
    );

    res.json({ success: true });
  } catch (error) {
    console.error("Error saving acknowledgment:", error);
    res.status(500).json({ error: "Failed to save acknowledgment" });
  }
});

// Add new staff member
app.post("/api/staff", async (req, res) => {
  try {
    const { name, department, role } = req.body;

    // Input validation
    if (!name || !department || !role) {
      return res.status(400).json({
        success: false,
        message: "All fields are required",
      });
    }

    const query = "INSERT INTO staff (name, department, role) VALUES (?, ?, ?)";
    const [result] = await pool.execute(query, [name, department, role]);

    res.json({
      success: true,
      message: "Staff member added successfully",
      staffId: result.insertId,
    });
  } catch (error) {
    console.error("Error adding staff:", error);
    res.status(500).json({
      success: false,
      message: "Error adding staff member",
      error: error.message,
    });
  }
});

// Get all staff members
app.get("/api/staff", async (req, res) => {
  try {
    const [rows] = await pool.query("SELECT * FROM staff ORDER BY name");
    res.json(rows);
  } catch (error) {
    console.error("Error fetching staff:", error);
    res.status(500).json({ error: "Failed to fetch staff" });
  }
});

// Get all forms
app.get("/api/forms", async (req, res) => {
  try {
    const [rows] = await db.query(
      "SELECT * FROM information ORDER BY created_at DESC"
    );
    res.json(rows);
  } catch (error) {
    console.error("Error fetching forms:", error);
    res.status(500).json({
      success: false,
      message: "Error fetching forms",
      error: error.message,
    });
  }
});

// Error handling middleware
app.use((err, req, res, next) => {
  console.error(err.stack);
  res.status(500).json({
    success: false,
    message: "Internal Server Error",
    error:
      process.env.NODE_ENV === "development"
        ? err.message
        : "An error occurred",
  });
});

// Handle 404 errors
app.use((req, res) => {
  res.status(404).json({
    success: false,
    message: "Route not found",
  });
});

// Start server
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`🚀 Server is running on port ${PORT}`);
});

// Graceful shutdown
process.on("SIGTERM", () => {
  console.log("SIGTERM signal received: closing HTTP server");
  db.end(() => {
    console.log("Database connection closed");
    process.exit(0);
  });
});
