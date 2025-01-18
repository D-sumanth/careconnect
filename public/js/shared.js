class InfoManager {
  static async init() {
    await this.fetchAndDisplayCounts();
  }

  static async fetchAndDisplayCounts() {
    try {
      const response = await fetch('/api/counts');
      if (!response.ok) throw new Error('Failed to fetch counts');
      const data = await response.json();
      this.displayCounts(data);
    } catch (error) {
      console.error('Error fetching counts:', error);
      // Display default values if fetch fails
      this.displayCounts({ in_house: 0, new_admissions: 0 });
    }
  }

  static async updateCounts(inHouse, newAdmissions) {
    try {
      const response = await fetch('/api/update-counts', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          inHouse: parseInt(inHouse),
          newAdmissions: parseInt(newAdmissions)
        })
      });

      if (!response.ok) throw new Error('Failed to update counts');
      await this.fetchAndDisplayCounts();
    } catch (error) {
      console.error('Error updating counts:', error);
    }
  }

  static displayCounts(counts) {
    document.getElementById('in-house-display').textContent = counts.in_house;
    document.getElementById('new-admissions-display').textContent = counts.new_admissions;
  }
}