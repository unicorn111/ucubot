CREATE TABLE lesson_signal (
     id INT NOT NULL AUTO_INCREMENT,
     timestamp_ TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
     signal_type SMALLINT,
     user_id VARCHAR(128) NOT NULL,
     PRIMARY KEY (id)
);
