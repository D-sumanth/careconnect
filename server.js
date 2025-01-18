const express = require("express");
const cors = require("cors");
const path = require("path");

// Load environment variables
require("dotenv").config();

// Database connection
const db = require("./config/database");

const app = express();

// Middleware
app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(express.static("public"));
app.use(express.static(path.join(__dirname, 'public')));


// Test database connection route
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

// API routes for forms
app.post("/api/forms", async (req, res) => {
  try {
    console.log("Received form data:", req.body); // Debug log

    const {
      home,
      department,
      nameDesignation,
      information,
      authorizedBy,
      state,
      sendTo,
    } = req.body;

    const [result] = await db.query(
      `INSERT INTO information 
            (home, department, name_designation, information, authorized_by, state_type, send_to) 
            VALUES (?, ?, ?, ?, ?, ?, ?)`,
      [
        home,
        department,
        nameDesignation,
        information,
        authorizedBy,
        state,
        JSON.stringify(sendTo),
      ]
    );

    res.json({
      success: true,
      message: "Form submitted successfully",
      id: result.insertId,
    });
  } catch (error) {
    console.error("Error submitting form:", error);
    res.status(500).json({
      success: false,
      message: "Error submitting form",
      error: error.message,
    });
  }
});

// Route for dashboard.html
app.get("/dashboard", (req, res) => {
  res.sendFile(path.join(__dirname, "public", "dashboard.html"));
});

// Route for staff-dashboard.html
app.get("/staff-dashboard", (req, res) => {
  res.sendFile(path.join(__dirname, "public", "staff-dashboard.html"));
});

// Get all staff members
app.get("/api/staff", async (req, res) => {
  try {
    const [rows] = await db.query("SELECT * FROM staff ORDER BY name");
    console.log("Fetched staff:", rows); // Add this for debugging
    res.json(rows);
  } catch (error) {
    console.error("Error fetching staff:", error);
    res.status(500).json({ error: "Failed to fetch staff" });
  }
});

// Add this to server.js
app.get("/api/acknowledgments/:infoId", async (req, res) => {
  try {
    const [rows] = await db.query(
      `
            SELECT a.acknowledged_at, s.name as staff_name
            FROM tempstaff a
            JOIN staff s ON a.staff_id = s.id
            WHERE a.info_id = ?
            ORDER BY a.acknowledged_at DESC
        `,
      [req.params.infoId]
    );

    console.log("Fetched XXXXXXXXXXXXXXX:", rows); // Debug log
    res.json(rows);
  } catch (error) {
    console.error("Error fetching XXXXXXXXXXXXXXX:", error);
    res.status(500).json({ error: "Failed to fetch XXXXXXXXXXXXXXX" });
  }
});

app.get("/api/acknowledgment-status/:infoId", async (req, res) => {
  try {
    // Get total number of staff
    const [staffCount] = await db.query("SELECT COUNT(*) as total FROM staff");

    // Get number of XXXXXXXXXXXXXXX for this info
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
  const { infoId, staffId } = req.body;

  try {
    await db.query(
      "INSERT INTO tempstaff (info_id, staff_id, acknowledged_at) VALUES (?, ?, NOW())",
      [infoId, staffId]
    );

    res.json({ success: true });
  } catch (error) {
    console.error("Error saving acknowledgment:", error);
    res.status(500).json({ error: "Failed to save acknowledgment" });
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

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`🚀 Server is running on port ${PORT}`);
});
