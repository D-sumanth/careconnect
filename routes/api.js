const express = require("express");
const router = express.Router();
const pool = require("../config/database");

// Get counts
router.get("/counts", async (req, res) => {
  try {
    const [rows] = await pool.query("SELECT in_house, new_admissions FROM counts WHERE id = 1");
    res.json(rows[0]);
  } catch (error) {
    console.error("Error fetching counts:", error);
    res.status(500).json({ error: "Failed to fetch counts" });
  }
});

// Update counts
router.post("/update-counts", async (req, res) => {
  try {
    const { inHouse, newAdmissions } = req.body;
    await pool.query(
      "UPDATE counts SET in_house = ?, new_admissions = ? WHERE id = 1",
      [inHouse, newAdmissions]
    );
    res.json({ success: true });
  } catch (error) {
    console.error("Error updating counts:", error);
    res.status(500).json({ error: "Failed to update counts" });
  }
});

module.exports = router;


router.post("/save-info", async (req, res) => {
  try {
    const {
      home,
      department,
      nameDesignation,
      information,
      authorizedBy,
      state,
      sendTo,
    } = req.body;

    console.log("Received data:", req.body); // Debug log

    const query = `
            INSERT INTO information 
            (home, department, name_designation, information, authorized_by, state_type, send_to) 
            VALUES (?, ?, ?, ?, ?, ?, ?)
        `;

    const values = [
      home,
      department,
      nameDesignation,
      information,
      authorizedBy,
      state,
      sendTo,
    ];

    console.log("Executing query:", { query, values }); // Debug log

    const [result] = await pool.execute(query, values);
    console.log("Insert result:", result); // Debug log

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
