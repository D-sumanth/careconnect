// shared.js
class InfoManager {
  static async init() {
    await this.fetchAndDisplayCounts();
    this.setupEventListeners();
  }

  static async fetchAndDisplayCounts() {
    try {
      const response = await fetch("/api/counts");
      if (!response.ok) throw new Error("Failed to fetch counts");
      const data = await response.json();
      console.log("Fetched counts:", data); // Debug log
      this.displayCounts(data);
      return data;
    } catch (error) {
      console.error("Error fetching counts:", error);
      this.displayCounts({ in_house: 0, new_admissions: 0 });
      return null;
    }
  }

  static displayCounts(counts) {
    const inHouseDisplay = document.getElementById("in-house-display");
    const newAdmissionsDisplay = document.getElementById(
      "new-admissions-display"
    );

    if (inHouseDisplay && newAdmissionsDisplay) {
      inHouseDisplay.textContent = counts.in_house;
      newAdmissionsDisplay.textContent = counts.new_admissions;
      console.log("Counts displayed:", counts); // Debug log
    }
  }

  static async updateCounts(inHouse, newAdmissions) {
    try {
      console.log("Updating counts:", { inHouse, newAdmissions }); // Debug log
      const response = await fetch("/api/update-counts", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          inHouse: parseInt(inHouse),
          newAdmissions: parseInt(newAdmissions),
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || "Failed to update counts");
      }

      const result = await response.json();
      console.log("Update response:", result); // Debug log

      // Fetch and display updated counts
      await this.fetchAndDisplayCounts();
      return true;
    } catch (error) {
      console.error("Error updating counts:", error);
      return false;
    }
  }

  static setupEventListeners() {
    // Remove any existing event listeners
    const updateForm = document.getElementById("updateForm");
    const popup = document.getElementById("updatePopup");

    if (updateForm) {
      // Remove old event listeners by cloning the form
      const newForm = updateForm.cloneNode(true);
      updateForm.parentNode.replaceChild(newForm, updateForm);

      // Add new submit event listener to the form
      newForm.addEventListener("submit", async (e) => {
        e.preventDefault(); // Prevent form from submitting traditionally

        const inHouseCount = document.getElementById("inHouseCount");
        const newAdmissions = document.getElementById("newAdmissions");

        if (!inHouseCount || !newAdmissions) {
          console.error("Required input fields not found");
          return;
        }

        // Validate inputs
        if (inHouseCount.value < 0 || newAdmissions.value < 0) {
          alert("Please enter valid numbers");
          return;
        }

        const success = await this.updateCounts(
          inHouseCount.value,
          newAdmissions.value
        );

        if (success) {
          popup.classList.remove("active");
          // Clear input fields
          inHouseCount.value = "";
          newAdmissions.value = "";
        } else {
          alert("Failed to update counts. Please try again.");
        }
      });
    }
  }
}
