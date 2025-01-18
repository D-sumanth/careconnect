const mysql = require('mysql2');

const connection = mysql.createConnection({
  host: 'localhost',
  user: 'root',
  password: 'Neil#1306s',
  database: 'careconnect',
});

connection.connect((err) => {
  if (err) {
    console.error('Connection error:', err);
  } else {
    console.log('Database connected successfully.');
  }
});

connection.end();
