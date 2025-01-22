const express = require("express");
const router = express.Router();
const pool = require("../config/database");

// Get current counts
router.get("/counts", async (req, res) => {
  try {
    const [rows] = await pool.query(
      `SELECT in_house, new_admissions, created_at, updated_at
       FROM counts 
       ORDER BY created_at DESC 
       LIMIT 1`
    );
    console.log("Fetched rows:", rows);

    if (rows.length === 0) {
      // If no counts exist, create initial record
      const [insertResult] = await pool.query(
        `INSERT INTO counts (in_house, new_admissions) VALUES (0, 0)`
      );
      return res.json({
        in_house: 0,
        new_admissions: 0,
        created_at: new Date(),
        updated_at: new Date(),
      });
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
router.post("/update-counts", async (req, res) => {
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

    const [result] = await pool.query(
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
        created_at: new Date(),
        updated_at: new Date(),
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

// Get counts history
router.get("/counts/history", async (req, res) => {
  try {
    const [rows] = await pool.query(
      `SELECT in_house, new_admissions, created_at, updated_at
       FROM counts 
       ORDER BY created_at DESC 
       LIMIT 10`
    );
    res.json(rows);
  } catch (error) {
    console.error("Error fetching counts history:", error);
    res.status(500).json({
      success: false,
      message: "Error fetching counts history",
      error: error.message,
    });
  }
});

// Keep the existing save-info route
router.post("/save-info", async (req, res) => {
  try {
    const {
      job_id,
      home,
      department,
      name,
      designation,
      information,
      authorized_by,
      send_to, // This will now be a simple string
      type,
      status,
    } = req.body;

    const query = `
      INSERT INTO form_submissions 
      (job_id, home, department, name, designation, information, authorized_by, send_to, type, status) 
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    `;

    const values = [
      job_id,
      home,
      department,
      name,
      designation,
      information,
      authorized_by,
      send_to, // Will be inserted as a plain string
      type,
      status,
    ];

    const [result] = await pool.execute(query, values);

    res.json({
      success: true,
      message: "Information saved successfully",
      id: result.insertId,
    });
  } catch (error) {
    console.error("Error saving information:", error);
    res.status(500).json({
      success: false,
      error: error.message,
    });
  }
});

module.exports = router;
