// Sử dụng cú pháp import thay cho require
import fs from "fs";
import axios from "axios";
import jsdom from "jsdom";
const { JSDOM } = jsdom;

async function fetchDogecoinAddresses() {
  let currentPage = 1;
  let addresses = [];
  let hasData = true;

  while (hasData) {
    const url = `https://bitinfocharts.com/top-100-richest-dogecoin-addresses-${currentPage}.html`;
    try {
      const response = await axios.get(url);
      const text = response.data;
      const dom = new JSDOM(text);
      const rows = dom.window.document.querySelectorAll("#tblOne tbody tr");

      if (rows.length === 0) {
        hasData = false;
      } else {
        rows.forEach((row) => {
          debugger;
          const addressCell = row.querySelector("td:nth-child(2) a");
          if (addressCell) {
            addresses.push(addressCell.textContent);
            console.log(addresses.length + "-add:" + addressCell.textContent);
          }
        });
        currentPage++;
        if (currentPage % 10 === 0) {
          // Lưu địa chỉ vào file
          fs.appendFileSync("dogecoin_addresses.txt", addresses.join("\n"));
          console.log("Đã lưu địa chỉ Dogecoin vào dogecoin_addresses.txt");
          addresses = [];
        }
      }
    } catch (error) {
      console.error(`Failed to fetch data: ${error}`);
      hasData = false;
    }
  }
}

// Gọi hàm
await fetchDogecoinAddresses();
