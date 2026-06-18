async function fetchInformation() {
  try {
    const response = await fetch("/api/forms");
    const data = await response.json();

    // Clear existing content
    document.getElementById("criticalContent").innerHTML = "";
    document.getElementById("routineContent").innerHTML = "";
    document.getElementById("eventContent").innerHTML = "";

    // Process and display each piece of information
    data.forEach((info) => {
      const infoBox = createInfoBox(info);

      switch (info.state_type.toLowerCase()) {
        case "critical":
          document.getElementById("criticalContent").appendChild(infoBox);
          break;
        case "routine":
          document.getElementById("routineContent").appendChild(infoBox);
          break;
        case "event-based":
          document.getElementById("eventContent").appendChild(infoBox);
          break;
      }
    });
  } catch (error) {
    console.error("Error fetching information:", error);
  }
}

async function populateStaffNames(selectElement) {
  try {
    console.log("Fetching staff names..."); // Debug log
    const response = await fetch("/api/staff");
    const staff = await response.json();
    console.log("Received staff data:", staff); // Debug log

    selectElement.innerHTML = '<option value="">Select Staff Name</option>';
    staff.forEach((person) => {
      const option = document.createElement("option");
      option.value = person.id;
      option.textContent = person.name;
      selectElement.appendChild(option);
    });
  } catch (error) {
    console.error("Error fetching staff names:", error);
  }
}

function createInfoBox(info) {
  const box = document.createElement("div");
  box.className = "info-box-item";
  box.dataset.id = info.id;

  const maxLength = 100;
  const truncatedText =
    info.information.length > maxLength
      ? info.information.substring(0, maxLength) + "..."
      : info.information;

  box.innerHTML = `
        <div class="info-header">
            <span class="info-date">${new Date(
              info.created_at
            ).toLocaleDateString()}</span>
            <span class="info-time">${new Date(
              info.created_at
            ).toLocaleTimeString()}</span>
        </div>
        <div class="info-body">
            <p class="info-text">${truncatedText}</p>
            <div class="info-meta">
                <span class="info-home">${info.home}</span>
                <span class="info-department">${info.department}</span>
            </div>
        </div>
        <div class="info-footer">
            <span class="info-author">By: ${info.name_designation}</span>
            <div class="button-group">
                <button class="acknowledge-btn" onclick="showModal('${encodeURIComponent(
                  JSON.stringify(info)
                )}', true)">
                    Acknowledge
                </button>
                <button class="view-more-btn" onclick="showModal('${encodeURIComponent(
                  JSON.stringify(info)
                )}', false)">
                    View More
                </button>
            </div>
        </div>
    `;

  // Check acknowledgment status
  checkAcknowledgmentStatus(info.id, box);

  return box;
}

async function checkAcknowledgmentStatus(infoId, boxElement) {
  try {
    const response = await fetch(`/api/acknowledgment-status/${infoId}`);
    const status = await response.json();

    if (status.isFullyAcknowledged) {
      boxElement.classList.add("fully-acknowledged");
    } else {
      boxElement.classList.remove("fully-acknowledged");
    }
  } catch (error) {
    console.error("Error checking acknowledgment status:", error);
  }
}

function showModal(infoString, isAcknowledge) {
  const info = JSON.parse(decodeURIComponent(infoString));

  const modal = document.createElement("div");
  modal.className = "modal";
  modal.innerHTML = `
        <div class="modal-content">
            <span class="close-modal">&times;</span>
            <h2>${info.home} - ${info.department}</h2>
            <div class="modal-info">
                <p class="full-info">${info.information}</p>
                <div class="info-details">
                    <p><strong>Author:</strong> ${info.name_designation}</p>
                    <p><strong>Authorized By:</strong> ${info.authorized_by}</p>
                    <p><strong>Date:</strong> ${new Date(
                      info.created_at
                    ).toLocaleString()}</p>
                    <p><strong>Send To:</strong> ${info.send_to}</p>
                </div>
                ${
                  isAcknowledge
                    ? `
                    <div class="acknowledge-section">
                        <select class="staff-select">
                            <option value="">Select Staff Name</option>
                        </select>
                        <button class="modal-acknowledge-btn" onclick="acknowledgeInfo('${info.id}', this)">
                            Acknowledge
                        </button>
                    </div>
                `
                    : `
                    <div class="acknowledgments-section">
                        <h3>Acknowledgments</h3>
                        <div class="acknowledgments-list">
                            <div class="loading-spinner">Loading XXXXXXXXXXXXXXX...</div>
                        </div>
                    </div>
                `
                }
            </div>
        </div>
    `;

  document.body.appendChild(modal);

  if (isAcknowledge) {
    const staffSelect = modal.querySelector(".staff-select");
    populateStaffNames(staffSelect);
  } else {
    const acknowledgmentsList = modal.querySelector(".acknowledgments-list");
    if (acknowledgmentsList) {
      loadAcknowledgments(info.id, acknowledgmentsList);
    }
  }

  // Close modal functionality
  const closeBtn = modal.querySelector(".close-modal");
  closeBtn.onclick = () => modal.remove();
  modal.onclick = (e) => {
    if (e.target === modal) modal.remove();
  };
}

async function loadAcknowledgments(infoId, container) {
  try {
    console.log("Fetching XXXXXXXXXXXXXXX for info ID:", infoId);
    const response = await fetch(`/api/acknowledgments/${infoId}`);
    const XXXXXXXXXXXXXXX = await response.json();

    console.log("Received XXXXXXXXXXXXXXX:", XXXXXXXXXXXXXXX);

    if (XXXXXXXXXXXXXXX.length === 0) {
      container.innerHTML =
        '<p class="no-acknowledgments">No XXXXXXXXXXXXXXX yet</p>';
      return;
    }

    container.innerHTML = `
            <div class="acknowledgments-wrapper">
                <table class="acknowledgments-table">
                    <thead>
                        <tr>
                            <th>Staff Name</th>
                            <th>Date & Time</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${XXXXXXXXXXXXXXX.map(
                          (ack) => `
                            <tr>
                                <td>${ack.staff_name}</td>
                                <td>${new Date(
                                  ack.acknowledged_at
                                ).toLocaleString()}</td>
                            </tr>
                        `
                        ).join("")}
                    </tbody>
                </table>
            </div>
        `;
  } catch (error) {
    console.error("Error loading XXXXXXXXXXXXXXX:", error);
    container.innerHTML =
      '<p class="error-message">Error loading XXXXXXXXXXXXXXX</p>';
  }
}

async function acknowledgeInfo(infoId, button) {
  const modal = button.closest(".modal-content");
  const staffSelect = modal.querySelector(".staff-select");
  const staffId = staffSelect.value;

  if (!staffId) {
    alert("Please select your name before acknowledging.");
    return;
  }

  try {
    const response = await fetch("/api/acknowledge", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        infoId,
        staffId,
      }),
    });

    const result = await response.json();
    if (result.success) {
      alert("Information acknowledged successfully!");
      modal.closest(".modal").remove();

      // Check acknowledgment status for the info box
      const infoBox = document.querySelector(`[data-id="${infoId}"]`);
      if (infoBox) {
        checkAcknowledgmentStatus(infoId, infoBox);
      }
    }
  } catch (error) {
    console.error("Error acknowledging information:", error);
    alert("Failed to acknowledge information");
  }
}
document.addEventListener('DOMContentLoaded', async () => {
  // Any staff dashboard-specific initialization code
  
  // Set up automatic refresh of counts every 30 seconds
  setInterval(() => {
    InfoManager.fetchAndDisplayCounts();
  }, 30000);
});


// Initial load
fetchInformation();
